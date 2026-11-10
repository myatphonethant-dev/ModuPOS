using ModuPOS.FeatureManagement.Features.CustomerFeatures;

namespace ModuPOS.FeatureManagement.Interfaces;

public interface ICustomerFeatureRepository
{
    Task<CustomerFeature?> GetAsync(
    Guid customerId,
    string featureCode,
    CancellationToken cancellationToken = default);
}