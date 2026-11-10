namespace ModuPOS.LicenseManagement.Models;

public sealed class LicensePayload
{
    public required string LicenseId { get; init; }

    public required Guid CustomerId { get; init; }

    public required string InstallationId { get; init; }

    public required string LicenseType { get; init; }

    public required List<string> Features { get; init; }

    public DateTime IssuedAt { get; init; }

    public DateTime ExpiresAt { get; init; }
}