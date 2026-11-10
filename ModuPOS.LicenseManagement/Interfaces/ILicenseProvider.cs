using ModuPOS.LicenseManagement.Models;

namespace ModuPOS.LicenseManagement.Interfaces;

public interface ILicenseProvider
{
    Task<bool> IsFeatureLicensedAsync(
        string featureCode,
        CancellationToken cancellationToken = default);

    Task<LicensePayload?> GetCurrentLicenseAsync(
        CancellationToken cancellationToken = default);
}