using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Authorization;
using ModuPOS.Organization.Entities;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Sessions;

/// <summary>
/// Ends sessions when an account or business stops being active (T058, FR-026). Admin features
/// call the Set* methods; the access pipeline calls <see cref="RevokeForUserAsync"/> and
/// <see cref="RevokeForBusinessAsync"/> when it notices a status change on its own.
/// </summary>
public sealed class SessionRevocationService(
    DbContext db,
    SessionStatusCache sessionCache,
    AuthorizationVersionService versions,
    IAuditWriter audit,
    ITenantContext caller,
    TimeProvider time)
{
    /// <summary>Changes an account's status; when it is no longer active, all its sessions end.</summary>
    public async Task<int> SetAccountStatusAsync(Guid userId, AccountStatus status, CancellationToken ct)
    {
        var account = await db.Set<UserAccount>().FirstAsync(a => a.Id == userId, ct);
        account.Status = status;
        await db.SaveChangesAsync(ct);

        foreach (var businessId in await BusinessesOfAsync(userId, ct))
            await versions.IncrementAsync(businessId, ct);

        return status == AccountStatus.Active ? 0 : await RevokeForUserAsync(userId, status, ct);
    }

    /// <summary>Changes a business's status; when it is no longer active, every session working in it ends.</summary>
    public async Task<int> SetBusinessStatusAsync(Guid businessId, BusinessStatus status, CancellationToken ct)
    {
        var business = await db.Set<Business>().FirstAsync(b => b.Id == businessId, ct);
        business.Status = status;
        await db.SaveChangesAsync(ct);
        await versions.IncrementAsync(businessId, ct);

        return status == BusinessStatus.Active ? 0 : await RevokeForBusinessAsync(businessId, status, null, ct);
    }

    public Task<int> RevokeForUserAsync(Guid userId, AccountStatus status, CancellationToken ct) =>
        RevokeAsync(
            db.Set<Session>().Where(s => s.UserAccountId == userId),
            SessionRevokedReason.AccountSuspended,
            $"account_{status.ToString().ToLowerInvariant()}",
            "UserAccount",
            userId.ToString(),
            ct);

    /// <summary>Ends sessions whose active profile belongs to the business (optionally only one user's).</summary>
    public Task<int> RevokeForBusinessAsync(Guid businessId, BusinessStatus status, Guid? onlyUserId, CancellationToken ct)
    {
        var profiles = db.Set<Profile>().Where(p => p.BusinessId == businessId).Select(p => p.Id);
        var sessions = db.Set<Session>().Where(s => s.ActiveProfileId != null && profiles.Contains(s.ActiveProfileId.Value));
        if (onlyUserId is { } uid)
            sessions = sessions.Where(s => s.UserAccountId == uid);

        return RevokeAsync(
            sessions,
            SessionRevokedReason.BusinessSuspended,
            $"business_{status.ToString().ToLowerInvariant()}",
            "Business",
            businessId.ToString(),
            ct);
    }

    private async Task<int> RevokeAsync(
        IQueryable<Session> query,
        SessionRevokedReason reason,
        string cause,
        string targetType,
        string targetId,
        CancellationToken ct)
    {
        var sessions = await query.Where(s => s.Status == SessionStatus.Active).ToListAsync(ct);
        if (sessions.Count == 0)
            return 0;

        foreach (var session in sessions)
        {
            session.Status = SessionStatus.Revoked;
            session.RevokedReason = reason;
            session.ActiveBranchId = null;
        }

        await db.SaveChangesAsync(ct);

        foreach (var session in sessions)
            await sessionCache.InvalidateAsync(session.Id, ct);

        audit.Enqueue(new AuditEntry
        {
            Code = AuditCodes.SessionsRevoked,
            Severity = AuditSeverity.Warning,
            OccurredAt = time.GetUtcNow().UtcDateTime,
            ActorUserId = caller.UserId,
            ActorProfileId = caller.ProfileId,
            BusinessId = targetType == "Business" ? Guid.Parse(targetId) : caller.BusinessId,
            SessionId = caller.SessionId,
            TargetType = targetType,
            TargetId = targetId,
            Details = new Dictionary<string, object?> { ["cause"] = cause, ["sessionsEnded"] = sessions.Count }
        });

        return sessions.Count;
    }

    private Task<List<Guid>> BusinessesOfAsync(Guid userId, CancellationToken ct) =>
        db.Set<Profile>()
            .Where(p => p.UserAccountId == userId && p.BusinessId != null)
            .Select(p => p.BusinessId!.Value)
            .Distinct()
            .ToListAsync(ct);
}
