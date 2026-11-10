using ModuPOS.FeatureManagement.Interfaces;

namespace ModuPOS.FeatureManagement.Features.CustomerFeatures;

public sealed class CustomerFeatureService(
    ICustomerFeatureRepository repository)
{
    public Task<CustomerFeature?> GetAsync(
        Guid customerId,
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        return repository.GetAsync(
            customerId,
            featureCode,
            cancellationToken);
    }
}