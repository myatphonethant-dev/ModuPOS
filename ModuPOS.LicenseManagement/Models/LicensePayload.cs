namespace ModuPOS.LicenseManagement.Models;

public class LicensePayload
{
    public required string LicenseId { get; set; }

    public required string CustomerId { get; set; }

    public required string InstallationId { get; set; }

    public required string LicenseType { get; set; }

    public required List<string> Features { get; set; }

    public DateTime IssuedAt { get; set; }

    public DateTime ExpiresAt { get; set; }
}