using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Auditing;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Sessions;

/// <summary>Ends one or all sessions; revocation is visible on the next request (FR-008, FR-009).</summary>
public sealed class SignOutService(
    DbContext db,
    SessionStatusCache sessionCache,
    IAuditWriter audit,
    ITenantContext caller,
    TimeProvider time)
{
    public async Task SignOutAsync(RequestInfo request, CancellationToken ct)
    {
        var session = await db.Set<Session>().FirstOrDefaultAsync(s => s.Id == caller.SessionId, ct);
        if (session is null)
            return;

        Revoke(session, SessionRevokedReason.SignOut);
        await db.SaveChangesAsync(ct);
        await sessionCache.InvalidateAsync(session.Id, ct);

        audit.Enqueue(Entry(AuditCodes.SignOut, request, new Dictionary<string, object?>()));
    }

    public async Task<int> SignOutAllAsync(RequestInfo request, CancellationToken ct)
    {
        var sessions = await db.Set<Session>()
            .Where(s => s.UserAccountId == caller.UserId && s.Status == SessionStatus.Active)
            .ToListAsync(ct);

        foreach (var session in sessions)
            Revoke(session, SessionRevokedReason.SignOutAll);

        await db.SaveChangesAsync(ct);

        foreach (var session in sessions)
            await sessionCache.InvalidateAsync(session.Id, ct);

        audit.Enqueue(Entry(AuditCodes.SignOutAll, request, new Dictionary<string, object?> { ["sessionsEnded"] = sessions.Count }));
        return sessions.Count;
    }

    private static void Revoke(Session session, SessionRevokedReason reason)
    {
        session.Status = SessionStatus.Revoked;
        session.RevokedReason = reason;
        session.ActiveBranchId = null;
    }

    private AuditEntry Entry(string code, RequestInfo request, Dictionary<string, object?> details) => new()
    {
        Code = code,
        OccurredAt = time.GetUtcNow().UtcDateTime,
        ActorUserId = caller.UserId,
        ActorProfileId = caller.ProfileId,
        BusinessId = caller.BusinessId,
        BranchId = caller.BranchId,
        SessionId = caller.SessionId,
        TargetType = "Session",
        TargetId = caller.SessionId?.ToString(),
        SourceAddress = request.SourceAddress,
        DeviceDescription = request.DeviceDescription,
        Details = details
    };
}
