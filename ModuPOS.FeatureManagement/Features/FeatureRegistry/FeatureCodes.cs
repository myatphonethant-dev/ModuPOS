namespace ModuPOS.FeatureManagement.Features.FeatureRegistry;

public static class FeatureCodes
{
    public const string Auth = "POS.AUTH";
    public const string Sale = "POS.SALE";
    public const string Inventory = "POS.INVENTORY";
    public const string Loyalty = "POS.LOYALTY";
    public const string Reporting = "POS.REPORTING";

    private static readonly HashSet<string> RegisteredCodes =
    [
        Auth,
        Sale,
        Inventory,
        Loyalty,
        Reporting
    ];

    public static bool IsKnown(string featureCode)
    {
        return !string.IsNullOrWhiteSpace(featureCode) &&
               RegisteredCodes.Contains(featureCode);
    }
}