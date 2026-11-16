namespace ModuPOS.LicenseManagement.Interfaces;

public interface ITenantLicenseStatus
{
    Task<bool> IsValidAsync(Guid businessId, CancellationToken cancellationToken = default);
}
