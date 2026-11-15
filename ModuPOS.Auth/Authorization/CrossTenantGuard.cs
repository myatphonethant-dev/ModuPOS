using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Roles;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Authorization;

/// <summary>
/// When something is "not found" in the caller's business, checks whether it exists in another
/// business and, if so, records <c>ACCESS.CROSS_TENANT_DENIED</c> (High). The caller still only
/// sees "not found", so existence is never revealed (FR-021, SC-004).
/// </summary>
public sealed class CrossTenantGuard(DbContext db, ITenantContext caller, IAuditWriter audit, TimeProvider time)
{
    public async Task CheckRoleAsync(Guid roleId, string action, CancellationToken ct) =>
        Report(await db.Set<Role>().IgnoreQueryFilters().Where(r => r.Id == roleId).Select(r => (Guid?)r.BusinessId).FirstOrDefaultAsync(ct),
            "Role", roleId, action);

    public async Task CheckRoleAssignmentAsync(Guid assignmentId, string action, CancellationToken ct) =>
        Report(await db.Set<RoleAssignment>().IgnoreQueryFilters().Where(a => a.Id == assignmentId).Select(a => (Guid?)a.BusinessId).FirstOrDefaultAsync(ct),
            "RoleAssignment", assignmentId, action);

    public async Task CheckProfileAsync(Guid profileId, string action, CancellationToken ct) =>
        Report(await db.Set<Profile>().Where(p => p.Id == profileId).Select(p => p.BusinessId).FirstOrDefaultAsync(ct),
            "Profile", profileId, action);

    /// <summary>Records the attempt when the target belongs to a business other than the caller's.</summary>
    public void Report(Guid? ownerBusinessId, string targetType, Guid targetId, string action)
    {
        if (ownerBusinessId is not { } foreign || foreign == caller.BusinessId)
            return;

        audit.Enqueue(new AuditEntry
        {
            Code = AuditCodes.CrossTenantDenied,
            Severity = AuditSeverity.High,
            Outcome = AuditOutcome.Denied,
            OccurredAt = time.GetUtcNow().UtcDateTime,
            ActorUserId = caller.UserId,
            ActorProfileId = caller.ProfileId,
            BusinessId = caller.BusinessId,
            BranchId = caller.BranchId,
            SessionId = caller.SessionId,
            TargetType = targetType,
            TargetId = targetId.ToString(),
            Details = new Dictionary<string, object?> { ["businessTargeted"] = foreign, ["action"] = action },
            DedupKey = $"{caller.UserId:N}:{AuditCodes.CrossTenantDenied}:{targetType}:{targetId:N}:{action}"
        });
    }
}
