using ModuPOS.Auth.Roles;
using ModuPOS.FeatureManagement.Features.FeatureRegistry;

namespace ModuPOS.Auth.Permissions;

public sealed record PermissionDefinition(
    string Code,
    string Name,
    string Group,
    string? FeatureCode,
    PermissionScope Scope = PermissionScope.Business,
    params string[] DependsOn);

/// <summary>Code constants for permissions, used in [RequirePermission(...)].</summary>
public static class PermissionCodes
{
    public const string RoleView = "role.view";
    public const string RoleManage = "role.manage";
    public const string RoleAssign = "role.assign";
    public const string AuditView = "audit.view";
    public const string PlatformAuditView = "platform.audit.view";
}

/// <summary>
/// The system permission catalog (see contracts/permission-catalog.md). Modules add their
/// permissions here as they are built; the catalog is synced to the database at startup.
/// </summary>
public static class PermissionCatalog
{
    public static readonly IReadOnlyList<PermissionDefinition> All =
    [
        new(PermissionCodes.RoleView, "View roles", "Roles", FeatureCodes.Auth),
        new(PermissionCodes.RoleManage, "Create and edit roles", "Roles", FeatureCodes.Auth, PermissionScope.Business, PermissionCodes.RoleView),
        new(PermissionCodes.RoleAssign, "Assign roles to staff", "Roles", FeatureCodes.Auth, PermissionScope.Business, PermissionCodes.RoleView),
        new(PermissionCodes.AuditView, "View audit log", "Audit", FeatureCodes.Auth),
        new(PermissionCodes.PlatformAuditView, "View business audit logs", "Platform", null, PermissionScope.Platform)
    ];
}
