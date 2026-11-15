namespace ModuPOS.Auth.Accounts;

public enum ProfileType
{
    PlatformAdmin,
    Staff,
    Member
}

public enum ProfileStatus
{
    Active,
    Inactive
}

/// <summary>A person's role in the system. Unique per (account, type, business).</summary>
public class Profile
{
    public Guid Id { get; set; }

    public Guid UserAccountId { get; set; }

    public ProfileType Type { get; set; }

    /// <summary>Required for Staff/Member, null for PlatformAdmin.</summary>
    public Guid? BusinessId { get; set; }

    public ProfileStatus Status { get; set; } = ProfileStatus.Active;
}
