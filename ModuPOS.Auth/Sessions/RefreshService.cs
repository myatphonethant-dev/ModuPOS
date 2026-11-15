using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Http;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Results;

namespace ModuPOS.Auth.Sessions;

/// <summary>Rotates renewal credentials; a reused credential revokes the whole session (FR-006, FR-007).</summary>
public sealed class RefreshService(
    DbContext db,
    SessionTokens tokens,
    SessionStatusCache sessionCache,
    IAuditWriter audit,
    IOptions<JwtOptions> options,
    TimeProvider time)
{
    public async Task<Result<TokenPair>> RefreshAsync(string refreshToken, RequestInfo request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            return AuthErrors.RefreshInvalid;

        var now = time.GetUtcNow().UtcDateTime;
        var hash = RefreshTokenEvaluator.Hash(refreshToken);

        var row = await db.Set<RefreshToken>()
            .Include(t => t.Session)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        if (row?.Session is null)
            return AuthErrors.RefreshInvalid;

        var session = row.Session;

        switch (RefreshTokenEvaluator.Evaluate(row, session, now))
        {
            case RefreshCheck.Invalid:
                return AuthErrors.RefreshInvalid;

            case RefreshCheck.Reused:
                return await RevokeForReuseAsync(session, request, now, ct);
        }

        // Claim the token atomically so two concurrent renewals cannot both succeed.
        var claimed = await db.Set<RefreshToken>()
            .Where(t => t.Id == row.Id && t.UsedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsedAt, now), ct);

        if (claimed == 0)
            return await RevokeForReuseAsync(session, request, now, ct);

        var (next, raw) = tokens.NewRefreshToken(session);
        row.ReplacedById = next.Id;
        session.LastSeenAt = now;
        session.ExpiresAt = now.AddDays(options.Value.RefreshTokenDays);
        await db.SaveChangesAsync(ct);
        await sessionCache.InvalidateAsync(session.Id, ct); // expiry moved

        var access = await tokens.IssueAccessTokenAsync(session, ct);
        return new TokenPair(access.Token, access.ExpiresAt, raw, next.ExpiresAt);
    }

    private async Task<Error> RevokeForReuseAsync(Session session, RequestInfo request, DateTime now, CancellationToken ct)
    {
        if (session.Status == SessionStatus.Active)
        {
            session.Status = SessionStatus.Revoked;
            session.RevokedReason = SessionRevokedReason.RenewalReuse;
            session.ActiveBranchId = null;
            await db.SaveChangesAsync(ct);
            await sessionCache.InvalidateAsync(session.Id, ct);
        }

        audit.Enqueue(new AuditEntry
        {
            Code = AuditCodes.RenewalReuseDetected,
            Severity = AuditSeverity.High,
            Outcome = AuditOutcome.Blocked,
            OccurredAt = now,
            ActorUserId = session.UserAccountId,
            ActorProfileId = session.ActiveProfileId,
            SessionId = session.Id,
            TargetType = "Session",
            TargetId = session.Id.ToString(),
            SourceAddress = request.SourceAddress,
            DeviceDescription = request.DeviceDescription,
            Details = new Dictionary<string, object?> { ["sessionRevoked"] = session.Id }
        });

        return AuthErrors.RefreshReused;
    }
}
