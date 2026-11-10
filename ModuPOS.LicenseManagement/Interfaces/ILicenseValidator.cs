using ModuPOS.LicenseManagement.Models;

namespace ModuPOS.LicenseManagement.Interfaces;

public interface ILicenseValidator
{
    Task<LicenseValidationResult> ValidateAsync(
        string licenseToken,
        CancellationToken cancellationToken = default);
}