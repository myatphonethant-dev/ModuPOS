namespace ModuPOS.LicenseManagement.Services;

using ModuPOS.LicenseManagement.Interfaces;

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
