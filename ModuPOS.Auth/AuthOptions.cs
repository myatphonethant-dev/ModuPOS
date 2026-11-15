namespace ModuPOS.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ModuPOS";

    public string Audience { get; set; } = "ModuPOS.Api";

    /// <summary>ES256 (P-256) private key in PEM. Required unless <see cref="AllowEphemeralKey"/>.</summary>
    public string PrivateKeyPem { get; set; } = string.Empty;

    /// <summary>Development/test only: generate a throwaway key at startup.</summary>
    public bool AllowEphemeralKey { get; set; }

    public int AccessTokenMinutes { get; set; } = 15;

    public int RefreshTokenDays { get; set; } = 7;
}

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public LockoutOptions Lockout { get; set; } = new();

    public int SignInRateLimitPerMinute { get; set; } = 20;

    public AuditOptions Audit { get; set; } = new();
}

public sealed class AuditOptions
{
    /// <summary>Identical refusals within this window become one entry with a count (FR-038).</summary>
    public int DedupWindowSeconds { get; set; } = 60;
}

public sealed class LockoutOptions
{
    public int MaxFailures { get; set; } = 5;

    public int WindowMinutes { get; set; } = 15;

    public int LockMinutes { get; set; } = 15;
}
