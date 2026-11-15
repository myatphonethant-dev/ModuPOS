using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Permissions;

namespace ModuPOS.Auth.Roles;

public sealed record DefaultRoles(Guid OwnerRoleId, Guid ManagerRoleId, Guid CashierRoleId);

/// <summary>
/// Creates the default roles for a new business (FR-019): Owner (all permissions, protected),
/// Manager and Cashier (editable), and gives the first staff member the Owner role.
/// </summary>
public sealed class DefaultRolesProvisioner(DbContext db)
{
    public static readonly IReadOnlyList<string> ManagerPermissions =
        [PermissionCodes.RoleView, PermissionCodes.RoleAssign, PermissionCodes.AuditView];

    public static readonly IReadOnlyList<string> CashierPermissions = [];

    public async Task<DefaultRoles> ProvisionAsync(Guid businessId, Guid ownerStaffProfileId, CancellationToken ct)
    {
        var owner = new Role { BusinessId = businessId, Name = "Owner", IsOwner = true, IsSystem = true };
        var manager = new Role { BusinessId = businessId, Name = "Manager", IsSystem = true };
        var cashier = new Role { BusinessId = businessId, Name = "Cashier", IsSystem = true };
        db.Set<Role>().AddRange(owner, manager, cashier);

        manager.Permissions.AddRange(ManagerPermissions.Select(code => new RolePermission { PermissionCode = code }));
        cashier.Permissions.AddRange(CashierPermissions.Select(code => new RolePermission { PermissionCode = code }));

        if (!await db.Set<AuthorizationVersion>().IgnoreQueryFilters().AnyAsync(v => v.BusinessId == businessId, ct))
            db.Set<AuthorizationVersion>().Add(new AuthorizationVersion { BusinessId = businessId, Value = 1 });

        db.Set<RoleAssignment>().Add(new RoleAssignment
        {
            BusinessId = businessId,
            StaffProfileId = ownerStaffProfileId,
            RoleId = owner.Id
        });

        await db.SaveChangesAsync(ct);
        return new DefaultRoles(owner.Id, manager.Id, cashier.Id);
    }
}
