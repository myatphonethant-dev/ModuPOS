using ModuPOS.FeatureManagement.Interfaces;

namespace ModuPOS.FeatureManagement.Features.CustomerFeatures;

public class CustomerFeatureService(
ICustomerFeatureRepository repository) : IFeatureManager
{
    public async Task<bool> IsEnabledAsync(
        Guid customerId,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        var customerFeature = await repository.GetAsync(
            customerId,
            featureCode,
            cancellationToken);

        if (customerFeature is null || !customerFeature.IsEnabled)
            return false;

        var now = DateTime.UtcNow;

        if (customerFeature.StartDate.HasValue &&
            now < customerFeature.StartDate.Value)
            return false;

        if (customerFeature.ExpiryDate.HasValue &&
            now >= customerFeature.ExpiryDate.Value)
            return false;

        return true;
    }
}