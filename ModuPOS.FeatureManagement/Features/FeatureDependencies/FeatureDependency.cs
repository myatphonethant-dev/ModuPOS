namespace ModuPOS.FeatureManagement.Features.FeatureDependencies;

public class FeatureDependency
{
    public required string FeatureCode { get; set; }

    public required string RequiredFeatureCode { get; set; }
}