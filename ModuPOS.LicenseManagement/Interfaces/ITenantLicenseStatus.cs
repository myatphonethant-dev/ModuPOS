namespace ModuPOS.LicenseManagement.Interfaces;

/// <summary>Whether a business (customer) currently holds a valid license.</summary>
public interface ITenantLicenseStatus
{
    Task<bool> IsValidAsync(Guid businessId, CancellationToken cancellationToken = default);
}
