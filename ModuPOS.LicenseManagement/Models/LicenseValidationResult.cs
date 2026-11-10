namespace ModuPOS.LicenseManagement.Models;

public class LicenseValidationResult
{
    public bool IsValid { get; set; }

    public string? ErrorMessage { get; set; }

    public LicensePayload? License { get; set; }

    public static LicenseValidationResult Success(LicensePayload license)
    {
        return new LicenseValidationResult
        {
            IsValid = true,
            License = license
        };
    }

    public static LicenseValidationResult Failed(string error)
    {
        return new LicenseValidationResult
        {
            IsValid = false,
            ErrorMessage = error
        };
    }
}