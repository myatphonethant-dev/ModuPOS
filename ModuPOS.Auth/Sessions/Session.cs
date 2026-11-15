namespace ModuPOS.Auth.Sessions;

public enum SessionStatus
{
    Active,
    Revoked,
    Expired
}

public enum SessionRevokedReason
{
    SignOut,
    SignOutAll,
    RenewalReuse,
    AccountSuspended,
    BusinessSuspended
}

/// <summary>One signed-in device. <see cref="Id"/> is the JWT <c>sid</c> claim.</summary>
public class Session
{
    public Guid Id { get; set; }

    public Guid UserAccountId { get; set; }

    public Guid? ActiveProfileId { get; set; }

    /// <summary>Staff only; cleared on sign-out.</summary>
    public Guid? ActiveBranchId { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    public SessionRevokedReason? RevokedReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    /// <summary>Last renewal + refresh lifetime (7 days).</summary>
    public DateTime ExpiresAt { get; set; }

    public string? SourceAddress { get; set; }

    public string? DeviceDescription { get; set; }

    public bool IsUsable(DateTime now) => Status == SessionStatus.Active && ExpiresAt > now;
}

/// <summary>Opaque renewal credential, stored only as a SHA-256 hash; one rotating family per session.</summary>
public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid SessionId { get; set; }

    public Session? Session { get; set; }

    public required byte[] TokenHash { get; set; }

    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }

    /// <summary>Set on rotation. Presenting a used token again revokes the session.</summary>
    public DateTime? UsedAt { get; set; }

    public Guid? ReplacedById { get; set; }
}
