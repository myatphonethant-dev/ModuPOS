namespace ModuPOS.FeatureManagement.Features.FeatureRegistry;

public class Feature
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Code { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}