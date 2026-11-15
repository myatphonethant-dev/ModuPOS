using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Http;
using ModuPOS.Organization.Entities;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Results;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Roles;

public sealed record RoleAssignmentView(Guid Id, Guid RoleId, string RoleName, Guid? BranchId, string? BranchName);

/// <summary>
/// Gives and removes roles for staff at business or branch scope (US4, FR-023–FR-027).
/// Every change bumps the authorization version and writes its audit entry in one transaction.
/// </summary>
public sealed class RoleAssignmentService(
    DbContext db,
    ITenantContext caller,
    EffectivePermissionService permissions,
    AuthorizationVersionService versions,
    IAuditWriter audit,
    CrossTenantGuard crossTenant,
    TimeProvider time)
{
    public async Task<Result<IReadOnlyList<RoleAssignmentView>>> ListAsync(Guid staffProfileId, CancellationToken ct)
    {
        if (!await IsStaffOfBusinessAsync(staffProfileId, "list_role_assignments", ct))
            return RbacErrors.StaffNotFound;

        var rows = await (
                from a in db.Set<RoleAssignment>().AsNoTracking()
                where a.StaffProfileId == staffProfileId
                join r in db.Set<Role>().AsNoTracking() on a.RoleId equals r.Id
                join b in db.Set<Branch>().AsNoTracking() on a.BranchId equals b.Id into br
                from b in br.DefaultIfEmpty()
                orderby r.Name, a.BranchId
                select new RoleAssignmentView(a.Id, r.Id, r.Name, a.BranchId, b == null ? null : b.Name))
            .ToListAsync(ct);

        return rows;
    }

    public async Task<Result<RoleAssignmentView>> GrantAsync(Guid staffProfileId, Guid roleId, Guid? branchId, CancellationToken ct)
    {
        if (!await IsStaffOfBusinessAsync(staffProfileId, "grant_role", ct))
            return RbacErrors.StaffNotFound;

        var role = await db.Set<Role>().AsNoTracking().Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
        {
            await crossTenant.CheckRoleAsync(roleId, "grant_role", ct);
            return RbacErrors.RoleNotFound;
        }

        if (role.IsOwner && branchId is not null)
            return RbacErrors.OwnerBusinessScopeOnly;

        string? branchName = null;
        if (branchId is { } bid)
        {
            var works = await db.Set<StaffBranchAssignment>().AnyAsync(s => s.StaffProfileId == staffProfileId && s.BranchId == bid, ct);
            if (!works)
                return RbacErrors.BranchNotAssigned;

            branchName = await db.Set<Branch>().Where(b => b.Id == bid).Select(b => b.Name).FirstOrDefaultAsync(ct);
        }

        if (await db.Set<RoleAssignment>().AnyAsync(a => a.StaffProfileId == staffProfileId && a.RoleId == roleId && a.BranchId == branchId, ct))
            return RbacErrors.AssignmentExists;

        // FR-017: nobody hands out a permission they do not hold. Owner means every permission.
        var granted = role.IsOwner
            ? (await permissions.CatalogForBusinessAsync(caller.BusinessId!.Value, ct)).Select(x => x.Permission.Code).ToList()
            : role.Permissions.Select(p => p.PermissionCode).ToList();
        var beyond = granted.Where(c => !caller.Permissions.Contains(c)).ToList();
        if (beyond.Count > 0)
        {
            audit.Enqueue(Entry(AuditCodes.EscalationBlocked, "RoleAssignment", null, new Dictionary<string, object?>
            {
                ["attempted"] = beyond.Order(StringComparer.Ordinal).ToArray(),
                ["roleId"] = role.Id,
                ["staffProfileId"] = staffProfileId
            }, AuditOutcome.Blocked, AuditSeverity.Warning));
            return RbacErrors.EscalationBlocked(beyond);
        }

        var assignment = new RoleAssignment
        {
            BusinessId = caller.BusinessId!.Value,
            StaffProfileId = staffProfileId,
            RoleId = role.Id,
            BranchId = branchId,
            AssignedAt = time.GetUtcNow().UtcDateTime,
            AssignedByProfileId = caller.ProfileId
        };
        db.Set<RoleAssignment>().Add(assignment);

        audit.WriteCritical(Entry(AuditCodes.RoleAssignmentGranted, "RoleAssignment", assignment.Id, Details(staffProfileId, role, branchId)));

        var error = await SaveWithVersionBumpAsync(ct);
        if (error is not null)
            return error;

        return new RoleAssignmentView(assignment.Id, role.Id, role.Name, branchId, branchName);
    }

    public async Task<Result<bool>> RevokeAsync(Guid staffProfileId, Guid assignmentId, CancellationToken ct)
    {
        var businessId = caller.BusinessId!.Value;

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        var assignment = await db.Set<RoleAssignment>()
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a => a.Id == assignmentId && a.StaffProfileId == staffProfileId, ct);
        if (assignment?.Role is null)
        {
            await crossTenant.CheckRoleAssignmentAsync(assignmentId, "revoke_role", ct);
            return RbacErrors.AssignmentNotFound;
        }

        if (assignment.Role.IsOwner && assignment.BranchId is null)
        {
            // Lock this business's Owner assignments so two admins cannot both remove "another" Owner.
            var otherOwners = await db.Database.SqlQuery<int>($"""
                SELECT COUNT(*) AS [Value]
                FROM [RoleAssignments] AS a WITH (UPDLOCK, HOLDLOCK)
                JOIN [Profiles] AS p ON p.[Id] = a.[StaffProfileId]
                JOIN [UserAccounts] AS u ON u.[Id] = p.[UserAccountId]
                WHERE a.[BusinessId] = {businessId}
                  AND a.[RoleId] = {assignment.RoleId}
                  AND a.[BranchId] IS NULL
                  AND a.[Id] <> {assignment.Id}
                  AND p.[Status] = 'Active'
                  AND u.[Status] = 'Active'
                """).SingleAsync(ct);

            if (otherOwners == 0)
            {
                audit.Enqueue(Entry(AuditCodes.OwnerProtectionBlocked, "RoleAssignment", assignment.Id, new Dictionary<string, object?>
                {
                    ["attempted"] = "remove_last_owner",
                    ["staffProfileId"] = staffProfileId
                }, AuditOutcome.Blocked, AuditSeverity.Warning));
                return RbacErrors.LastOwner;
            }
        }

        db.Set<RoleAssignment>().Remove(assignment);
        audit.WriteCritical(Entry(AuditCodes.RoleAssignmentRevoked, "RoleAssignment", assignment.Id, Details(staffProfileId, assignment.Role, assignment.BranchId)));

        try
        {
            await versions.BumpAsync(businessId, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return RbacErrors.AuditUnavailable;
        }

        await versions.InvalidateAsync(businessId, ct);
        return true;
    }

    /// <summary>Profiles are not tenant-filtered, so the business is checked explicitly.</summary>
    private async Task<bool> IsStaffOfBusinessAsync(Guid staffProfileId, string action, CancellationToken ct)
    {
        var ok = await db.Set<Profile>().AnyAsync(p => p.Id == staffProfileId && p.Type == ProfileType.Staff && p.BusinessId == caller.BusinessId, ct);
        if (!ok)
            await crossTenant.CheckProfileAsync(staffProfileId, action, ct);
        return ok;
    }

    private async Task<Error?> SaveWithVersionBumpAsync(CancellationToken ct)
    {
        var businessId = caller.BusinessId!.Value;
        try
        {
            await using var tx = await db.Database.BeginTransactionAsync(ct);
            await versions.BumpAsync(businessId, ct);
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        catch (DbUpdateException)
        {
            db.ChangeTracker.Clear();
            return RbacErrors.AuditUnavailable;
        }

        await versions.InvalidateAsync(businessId, ct);
        return null;
    }

    private static Dictionary<string, object?> Details(Guid staffProfileId, Role role, Guid? branchId) => new()
    {
        ["staffProfileId"] = staffProfileId,
        ["roleId"] = role.Id,
        ["roleName"] = role.Name,
        ["scope"] = branchId is null ? "business" : "branch",
        ["branchId"] = branchId
    };

    private AuditEntry Entry(
        string code,
        string targetType,
        Guid? targetId,
        Dictionary<string, object?> details,
        AuditOutcome outcome = AuditOutcome.Succeeded,
        AuditSeverity severity = AuditSeverity.Info) => new()
    {
        Code = code,
        Severity = severity,
        Outcome = outcome,
        OccurredAt = time.GetUtcNow().UtcDateTime,
        ActorUserId = caller.UserId,
        ActorProfileId = caller.ProfileId,
        BusinessId = caller.BusinessId,
        BranchId = caller.BranchId,
        SessionId = caller.SessionId,
        TargetType = targetType,
        TargetId = targetId?.ToString(),
        Details = details
    };
}
