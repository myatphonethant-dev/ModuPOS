namespace ModuPOS.Auth.Accounts;

public enum AccountStatus
{
    Active,
    Suspended,
    Deactivated
}

public class UserAccount
{
    public Guid Id { get; set; }

    /// <summary>Unique, 3–50 chars, case-insensitive.</summary>
    public required string Username { get; set; }

    /// <summary>Unique, case-insensitive.</summary>
    public required string Email { get; set; }

    /// <summary>PasswordHasher output. Never logged or audited.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    public AccountStatus Status { get; set; } = AccountStatus.Active;

    public int FailedSignInCount { get; set; }

    public DateTime? FailureWindowStart { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Profile> Profiles { get; set; } = [];
}
