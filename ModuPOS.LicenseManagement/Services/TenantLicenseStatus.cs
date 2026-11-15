using ModuPOS.LicenseManagement.Interfaces;

namespace ModuPOS.LicenseManagement.Services;

/// <summary>
/// Per-business view of the installation license: valid when the current license (cached by
/// <see cref="ILicenseProvider"/>) belongs to this business and has not expired.
/// </summary>
public sealed class TenantLicenseStatus(ILicenseProvider provider, TimeProvider time) : ITenantLicenseStatus
{
    public async Task<bool> IsValidAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var license = await provider.GetCurrentLicenseAsync(cancellationToken);

        return license is not null &&
               license.CustomerId == businessId &&
               license.ExpiresAt > time.GetUtcNow().UtcDateTime;
    }
}
