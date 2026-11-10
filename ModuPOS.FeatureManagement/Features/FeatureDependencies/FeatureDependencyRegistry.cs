using ModuPOS.FeatureManagement.Features.FeatureRegistry;

namespace ModuPOS.FeatureManagement.Features.FeatureDependencies;

public static class FeatureDependencyRegistry
{
    private static readonly Dictionary<string, string[]> Dependencies = new()
    {
        [FeatureCodes.Auth] = [],

        [FeatureCodes.Loyalty] = [ FeatureCodes.Sale ],

        [FeatureCodes.Reporting] = [ FeatureCodes.Sale ]
    };

    public static IReadOnlyList<string> GetDependencies(string featureCode)
    {
        return Dependencies.TryGetValue(featureCode, out var dependencies)
            ? dependencies
            : [];
    }
}