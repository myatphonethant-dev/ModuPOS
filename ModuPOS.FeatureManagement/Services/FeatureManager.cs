using ModuPOS.FeatureManagement.Features.FeatureDependencies;
using ModuPOS.FeatureManagement.Features.FeatureRegistry;
using ModuPOS.FeatureManagement.Interfaces;
using ModuPOS.LicenseManagement.Interfaces;

namespace ModuPOS.FeatureManagement.Services;

public sealed class FeatureManager(
    ICustomerFeatureRepository customerFeatureRepository,
    ILicenseProvider licenseProvider) : IFeatureManager
{
    public async Task<bool> IsEnabledAsync(
        Guid customerId,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty ||
            !FeatureCodes.IsKnown(featureCode))
        {
            return false;
        }

        var license = await licenseProvider.GetCurrentLicenseAsync(
            cancellationToken);

        if (license is null || license.CustomerId != customerId)
            return false;

        var licensedFeatures = license.Features.ToHashSet(
            StringComparer.Ordinal);

        return await IsAvailableAsync(
            customerId,
            featureCode,
            licensedFeatures,
            new HashSet<string>(StringComparer.Ordinal),
            cancellationToken);
    }

    private async Task<bool> IsAvailableAsync(
        Guid customerId,
        string featureCode,
        HashSet<string> licensedFeatures,
        HashSet<string> evaluationPath,
        CancellationToken cancellationToken)
    {
        if (!FeatureCodes.IsKnown(featureCode) ||
            !licensedFeatures.Contains(featureCode))
        {
            return false;
        }

        if (!evaluationPath.Add(featureCode))
            return false;

        try
        {
            var assignment = await customerFeatureRepository.GetAsync(
                customerId,
                featureCode,
                cancellationToken);

            if (assignment is null ||
                !assignment.IsEnabled ||
                assignment.Feature is null ||
                !assignment.Feature.IsActive)
            {
                return false;
            }

            var now = DateTime.UtcNow;

            if (assignment.StartDate.HasValue &&
                now < EnsureUtc(assignment.StartDate.Value))
            {
                return false;
            }

            if (assignment.ExpiryDate.HasValue &&
                now >= EnsureUtc(assignment.ExpiryDate.Value))
            {
                return false;
            }

            foreach (var dependency in
                     FeatureDependencyRegistry.GetDependencies(featureCode))
            {
                var dependencyAvailable = await IsAvailableAsync(
                    customerId,
                    dependency,
                    licensedFeatures,
                    evaluationPath,
                    cancellationToken);

                if (!dependencyAvailable)
                    return false;
            }

            return true;
        }
        finally
        {
            evaluationPath.Remove(featureCode);
        }
    }

    private static DateTime EnsureUtc(DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}