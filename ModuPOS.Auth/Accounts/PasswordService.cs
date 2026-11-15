using Microsoft.AspNetCore.Identity;

namespace ModuPOS.Auth.Accounts;

/// <summary>PBKDF2 password hashing via ASP.NET Core's PasswordHasher (FR-010).</summary>
public sealed class PasswordService
{
    private readonly PasswordHasher<UserAccount> _hasher = new();
    private readonly UserAccount _dummyAccount = new() { Username = "dummy", Email = "dummy@invalid" };
    private readonly string _dummyHash;

    public PasswordService() => _dummyHash = _hasher.HashPassword(_dummyAccount, Guid.NewGuid().ToString());

    public string Hash(UserAccount account, string password) => _hasher.HashPassword(account, password);

    /// <summary>Verifies; when the stored hash uses old settings it is upgraded in place.</summary>
    public bool Verify(UserAccount account, string password)
    {
        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, password);

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            account.PasswordHash = _hasher.HashPassword(account, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }

    /// <summary>Spends the same time as a real check, so unknown usernames are not revealed by timing.</summary>
    public void VerifyDummy(string password) =>
        _hasher.VerifyHashedPassword(_dummyAccount, _dummyHash, password);
}
