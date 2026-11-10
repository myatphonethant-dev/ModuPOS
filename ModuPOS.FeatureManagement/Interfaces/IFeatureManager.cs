namespace ModuPOS.FeatureManagement.Interfaces;

public interface IFeatureManager
{
    Task<bool> IsEnabledAsync(
        Guid customerId,
        string featureCode,
        CancellationToken cancellationToken = default);
}