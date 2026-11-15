using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Http;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Results;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Roles;

public sealed record RoleSummary(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsOwner,
    bool IsActive,
    int PermissionCount,
    int AssignedStaffCount,
    string Version);

public sealed record RoleDetail(
    Guid Id,
    string Name,
    string? Description,
    bool IsSystem,
    bool IsOwner,
    bool IsActive,
    IReadOnlyList<string> Permissions,
    string Version,
    IReadOnlyList<string> AddedDependencies);

public sealed record RoleUpdate(string Name, string? Description, IReadOnlyCollection<string> Permissions, string? Version);

/// <summary>
/// Role management for the caller's business (US3). Every change bumps the authorization
/// version and writes its audit entry in the same transaction; if either fails nothing changes.
/// </summary>
public sealed class RoleService(
    DbContext db,
    ITenantContext caller,
    EffectivePermissionService permissions,
    AuthorizationVersionService versions,
    IAuditWriter audit,
    CrossTenantGuard crossTenant,
    TimeProvider time)
{
    public async Task<IReadOnlyList<RoleSummary>> ListAsync(CancellationToken ct)
    {
        var ownerCount = (await permissions.CatalogForBusinessAsync(caller.BusinessId!.Value, ct)).Count;

        var rows = await db.Set<Role>()
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new
            {
                r.Id, r.Name, r.Description, r.IsSystem, r.IsOwner, r.IsActive, r.Version,
                PermissionCount = r.Permissions.Count,
                Staff = db.Set<RoleAssignment>().Where(a => a.RoleId == r.Id).Select(a => a.StaffProfileId).Distinct().Count()
            })
            .ToListAsync(ct);

        return rows.Select(r => new RoleSummary(
                r.Id, r.Name, r.Description, r.IsSystem, r.IsOwner, r.IsActive,
                r.IsOwner ? ownerCount : r.PermissionCount, r.Staff, Convert.ToBase64String(r.Version)))
            .ToList();
    }

    public async Task<Result<RoleDetail>> GetAsync(Guid roleId, CancellationToken ct)
    {
        // The tenant filter hides other businesses' roles: they are simply "not found".
        var role = await db.Set<Role>().AsNoTracking().Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
        {
            await crossTenant.CheckRoleAsync(roleId, "view_role", ct);
            return RbacErrors.RoleNotFound;
        }

        return await DetailAsync(role, [], ct);
    }

    public async Task<Result<RoleDetail>> CreateAsync(RoleDraft draft, CancellationToken ct)
    {
        var validated = await ValidateAsync(draft, ct);
        if (!validated.IsSuccess)
            return validated.Error!;

        var v = validated.Value!;
        if (await db.Set<Role>().AnyAsync(r => r.Name == v.Name, ct))
            return RbacErrors.RoleNameTaken;

        var role = new Role { BusinessId = caller.BusinessId!.Value, Name = v.Name, Description = v.Description };
        role.Permissions.AddRange(v.Permissions.Select(code => new RolePermission { PermissionCode = code }));
        db.Set<Role>().Add(role);

        audit.WriteCritical(Entry(AuditCodes.RoleCreated, role.Id, new Dictionary<string, object?>
        {
            ["name"] = role.Name,
            ["permissions"] = v.Permissions.Order(StringComparer.Ordinal).ToArray()
        }));

        var saved = await SaveWithVersionBumpAsync(ct);
        if (saved is not null)
            return saved;

        return await DetailAsync(role, v.AddedDependencies, ct);
    }

    public async Task<Result<RoleDetail>> UpdateAsync(Guid roleId, RoleUpdate update, CancellationToken ct)
    {
        var role = await db.Set<Role>().Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
        {
            await crossTenant.CheckRoleAsync(roleId, "update_role", ct);
            return RbacErrors.RoleNotFound;
        }

        if (role.IsOwner)
        {
            audit.Enqueue(Entry(AuditCodes.OwnerProtectionBlocked, role.Id, new Dictionary<string, object?>
            {
                ["attempted"] = "update",
                ["name"] = update.Name
            }, AuditOutcome.Blocked, AuditSeverity.Warning));
            return RbacErrors.OwnerProtected;
        }

        if (!TryParseVersion(update.Version, out var clientVersion) || !clientVersion.AsSpan().SequenceEqual(role.Version))
            return RbacErrors.VersionConflict;

        var validated = await ValidateAsync(new RoleDraft(update.Name, update.Description, update.Permissions), ct);
        if (!validated.IsSuccess)
            return validated.Error!;

        var v = validated.Value!;
        if (!string.Equals(v.Name, role.Name, StringComparison.OrdinalIgnoreCase) &&
            await db.Set<Role>().AnyAsync(r => r.Id != role.Id && r.Name == v.Name, ct))
        {
            return RbacErrors.RoleNameTaken;
        }

        var before = role.Permissions.Select(p => p.PermissionCode).ToHashSet(StringComparer.Ordinal);
        var added = v.Permissions.Except(before).Order(StringComparer.Ordinal).ToArray();
        var removed = before.Except(v.Permissions).Order(StringComparer.Ordinal).ToArray();

        if (role.Name != v.Name || role.Description != v.Description)
        {
            audit.WriteCritical(Entry(AuditCodes.RoleUpdated, role.Id, new Dictionary<string, object?>
            {
                ["before"] = new { name = role.Name, description = role.Description },
                ["after"] = new { name = v.Name, description = v.Description }
            }));
        }

        if (added.Length > 0 || removed.Length > 0)
        {
            var staffAffected = await db.Set<RoleAssignment>().Where(a => a.RoleId == role.Id).Select(a => a.StaffProfileId).Distinct().CountAsync(ct);
            audit.WriteCritical(Entry(AuditCodes.RolePermissionsChanged, role.Id, new Dictionary<string, object?>
            {
                ["added"] = added,
                ["removed"] = removed,
                ["staffAffected"] = staffAffected
            }));
        }

        role.Name = v.Name;
        role.Description = v.Description;
        role.Permissions.RemoveAll(p => removed.Contains(p.PermissionCode));
        role.Permissions.AddRange(added.Select(code => new RolePermission { RoleId = role.Id, PermissionCode = code }));

        // Always write the role row so its rowversion changes and is checked, even when only
        // its permissions changed; this is what makes the second of two concurrent edits fail.
        db.Entry(role).State = EntityState.Modified;
        db.Entry(role).Property(r => r.Version).OriginalValue = clientVersion;

        var saved = await SaveWithVersionBumpAsync(ct);
        if (saved is not null)
            return saved;

        return await DetailAsync(role, v.AddedDependencies, ct);
    }

    /// <summary>Turns a role off (it grants nothing, assignments stay) or back on. Returns staff affected.</summary>
    public async Task<Result<int>> SetActiveAsync(Guid roleId, bool active, CancellationToken ct)
    {
        var role = await db.Set<Role>().FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
        {
            await crossTenant.CheckRoleAsync(roleId, active ? "reactivate_role" : "deactivate_role", ct);
            return RbacErrors.RoleNotFound;
        }

        if (role.IsOwner)
            return BlockOwner(role, active ? "reactivate" : "deactivate");

        var affected = await StaffHoldingAsync(role.Id, ct);
        if (role.IsActive == active)
            return affected;

        role.IsActive = active;
        audit.WriteCritical(Entry(active ? AuditCodes.RoleReactivated : AuditCodes.RoleDeactivated, role.Id,
            new Dictionary<string, object?> { ["name"] = role.Name, ["staffAffected"] = affected }));

        var error = await SaveWithVersionBumpAsync(ct);
        return error is null ? affected : error;
    }

    /// <summary>Deletes a role nobody holds (FR-020). The Owner role is never deleted.</summary>
    public async Task<Result<bool>> DeleteAsync(Guid roleId, CancellationToken ct)
    {
        var role = await db.Set<Role>().Include(r => r.Permissions).FirstOrDefaultAsync(r => r.Id == roleId, ct);
        if (role is null)
        {
            await crossTenant.CheckRoleAsync(roleId, "delete_role", ct);
            return RbacErrors.RoleNotFound;
        }

        if (role.IsOwner)
            return BlockOwner(role, "delete");

        var holders = await (
                from a in db.Set<RoleAssignment>().AsNoTracking()
                where a.RoleId == role.Id
                join p in db.Set<Accounts.Profile>().AsNoTracking() on a.StaffProfileId equals p.Id
                join u in db.Set<Accounts.UserAccount>().AsNoTracking() on p.UserAccountId equals u.Id
                orderby u.Username
                select new { staffProfileId = a.StaffProfileId, name = u.Username, branchId = a.BranchId })
            .ToListAsync(ct);

        if (holders.Count > 0)
            return RbacErrors.RoleInUse(holders);

        audit.WriteCritical(Entry(AuditCodes.RoleDeleted, role.Id, new Dictionary<string, object?>
        {
            ["name"] = role.Name,
            ["permissions"] = role.Permissions.Select(p => p.PermissionCode).Order(StringComparer.Ordinal).ToArray()
        }));
        db.Set<Role>().Remove(role);

        var error = await SaveWithVersionBumpAsync(ct);
        return error is null ? true : error;
    }

    private Error BlockOwner(Role role, string attempted)
    {
        audit.Enqueue(Entry(AuditCodes.OwnerProtectionBlocked, role.Id,
            new Dictionary<string, object?> { ["attempted"] = attempted }, AuditOutcome.Blocked, AuditSeverity.Warning));
        return RbacErrors.OwnerProtected;
    }

    private Task<int> StaffHoldingAsync(Guid roleId, CancellationToken ct) =>
        db.Set<RoleAssignment>().Where(a => a.RoleId == roleId).Select(a => a.StaffProfileId).Distinct().CountAsync(ct);

    private async Task<Result<ValidatedRole>> ValidateAsync(RoleDraft draft, CancellationToken ct)
    {
        var businessId = caller.BusinessId!.Value;
        var available = (await permissions.CatalogForBusinessAsync(businessId, ct))
            .ToDictionary(x => x.Permission.Code, x => x.Available, StringComparer.Ordinal);

        var catalog = await db.Set<Permission>()
            .AsNoTracking()
            .ToDictionaryAsync(
                p => p.Code,
                p => new PermissionInfo(p.Code, p.DependsOn, p.Scope, p.IsRetired, available.GetValueOrDefault(p.Code)),
                StringComparer.Ordinal,
                ct);

        var result = RoleValidator.Validate(draft, catalog, caller.Permissions);

        if (result.Error is { Code: "escalation_blocked" } error)
        {
            audit.Enqueue(Entry(AuditCodes.EscalationBlocked, null, new Dictionary<string, object?>
            {
                ["attempted"] = error.Extensions?["permissions"],
                ["roleName"] = draft.Name
            }, AuditOutcome.Blocked, AuditSeverity.Warning));
        }

        return result;
    }

    /// <summary>Saves the unit of work and the version bump atomically. Returns an error, or null on success.</summary>
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
        catch (DbUpdateConcurrencyException)
        {
            db.ChangeTracker.Clear();
            return RbacErrors.VersionConflict;
        }
        catch (DbUpdateException)
        {
            // The change and its audit entry are one transaction (FR-040).
            db.ChangeTracker.Clear();
            return RbacErrors.AuditUnavailable;
        }

        await versions.InvalidateAsync(businessId, ct);
        return null;
    }

    private async Task<RoleDetail> DetailAsync(Role role, IReadOnlyList<string> addedDependencies, CancellationToken ct)
    {
        IReadOnlyList<string> codes = role.IsOwner
            ? (await permissions.CatalogForBusinessAsync(role.BusinessId, ct)).Where(x => x.Available).Select(x => x.Permission.Code).ToList()
            : role.Permissions.Select(p => p.PermissionCode).Order(StringComparer.Ordinal).ToList();

        return new RoleDetail(role.Id, role.Name, role.Description, role.IsSystem, role.IsOwner, role.IsActive,
            codes, Convert.ToBase64String(role.Version), addedDependencies);
    }

    private static bool TryParseVersion(string? value, out byte[] version)
    {
        version = [];
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            version = Convert.FromBase64String(value);
            return version.Length == 8;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private AuditEntry Entry(
        string code,
        Guid? roleId,
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
        TargetType = roleId is null ? null : "Role",
        TargetId = roleId?.ToString(),
        Details = details
    };
}
