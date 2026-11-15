using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth.Roles;

public enum PermissionScope
{
    Business,
    Platform
}

/// <summary>System-defined permission, synced from <see cref="Permissions.PermissionCatalog"/>.</summary>
public class Permission
{
    /// <summary>Lowercase <c>module.action</c>, e.g. <c>role.manage</c>.</summary>
    public required string Code { get; set; }

    public required string Name { get; set; }

    public required string Group { get; set; }

    /// <summary>Owning feature code; null for platform permissions.</summary>
    public string? FeatureCode { get; set; }

    public List<string> DependsOn { get; set; } = [];

    public PermissionScope Scope { get; set; } = PermissionScope.Business;

    public bool IsRetired { get; set; }
}

public class Role : ITenantOwned
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>2–60 chars, unique per business (case-insensitive).</summary>
    public required string Name { get; set; }

    /// <summary>≤ 500 chars.</summary>
    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    /// <summary>Exactly one per business; name/permissions immutable; not deletable.</summary>
    public bool IsOwner { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>SQL Server rowversion concurrency token (FR-022).</summary>
    public byte[] Version { get; set; } = [];

    public List<RolePermission> Permissions { get; set; } = [];
}

public class RolePermission
{
    public Guid RoleId { get; set; }

    public required string PermissionCode { get; set; }
}

public class RoleAssignment : ITenantOwned
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    public Guid StaffProfileId { get; set; }

    public Guid RoleId { get; set; }

    public Role? Role { get; set; }

    /// <summary>Null = business scope; otherwise must be one of the staff member's branches.</summary>
    public Guid? BranchId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

    public Guid? AssignedByProfileId { get; set; }
}

/// <summary>Incremented on any change that affects effective permissions in the business.</summary>
public class AuthorizationVersion : ITenantOwned
{
    public Guid BusinessId { get; set; }

    public long Value { get; set; }
}
