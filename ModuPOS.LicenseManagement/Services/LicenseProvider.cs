using Microsoft.Extensions.Options;
using ModuPOS.LicenseManagement.Interfaces;
using ModuPOS.LicenseManagement.Models;

namespace ModuPOS.LicenseManagement.Services;

public sealed class LicenseProvider(
    ILicenseValidator validator,
    IOptions<LicenseValidationOptions> options) : ILicenseProvider
{
    private readonly LicenseValidationOptions _options = options.Value;

    private readonly SemaphoreSlim _lock = new(1, 1);

    private LicensePayload? _cachedLicense;
    private string? _cachedToken;

    public async Task<bool> IsFeatureLicensedAsync(
        string featureCode,
        CancellationToken cancellationToken = default)
    {
        var license = await GetCurrentLicenseAsync(cancellationToken);

        return license?.Features.Contains(
            featureCode,
            StringComparer.Ordinal) == true;
    }

    public async Task<LicensePayload?> GetCurrentLicenseAsync(
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.Token))
            return null;

        await _lock.WaitAsync(cancellationToken);

        try
        {
            if (_cachedLicense is not null &&
                _cachedToken == _options.Token &&
                _cachedLicense.ExpiresAt > DateTime.UtcNow)
            {
                return _cachedLicense;
            }

            var result = await validator.ValidateAsync(
                _options.Token,
                cancellationToken);

            _cachedToken = _options.Token;

            _cachedLicense = result.IsValid
                ? result.License
                : null;

            return _cachedLicense;
        }
        finally
        {
            _lock.Release();
        }
    }
}