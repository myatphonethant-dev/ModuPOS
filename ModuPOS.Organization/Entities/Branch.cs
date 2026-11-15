using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Organization.Entities;

public enum BranchStatus
{
    Active,
    Inactive
}

public class Branch : ITenantOwned
{
    public Guid Id { get; set; }

    public Guid BusinessId { get; set; }

    /// <summary>Unique per business.</summary>
    public required string Name { get; set; }

    public BranchStatus Status { get; set; } = BranchStatus.Active;
}

/// <summary>Links a staff profile to a branch they may work in.</summary>
public class StaffBranchAssignment : ITenantOwned
{
    public Guid StaffProfileId { get; set; }

    public Guid BranchId { get; set; }

    public Guid BusinessId { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
