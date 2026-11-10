namespace ModuPOS.LicenseManagement.Models;

public sealed class LicenseValidationOptions
{
    public const string SectionName = "License";

    public string Issuer { get; set; } = "ModuPOS";

    public string Audience { get; set; } = "ModuPOS";

    public string PublicKeyPem { get; set; } = string.Empty;

    public string ExpectedInstallationId { get; set; } = string.Empty;

    public string Token { get; set; } = string.Empty;
}