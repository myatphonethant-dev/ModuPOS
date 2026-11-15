namespace ModuPOS.Organization.Entities;

public enum BusinessStatus
{
    Active,
    Suspended,
    Deactivated
}

/// <summary>A tenant. <see cref="Id"/> is the same value as FeatureManagement's customerId.</summary>
public class Business
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public BusinessStatus Status { get; set; } = BusinessStatus.Active;
}
