using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace ModuPOS.Auth.Sessions;

public enum RefreshCheck
{
    Valid,
    Invalid,
    Reused
}

/// <summary>Pure rules for renewal credentials: generation, hashing, and rotation checks.</summary>
public static class RefreshTokenEvaluator
{
    /// <summary>A used token presented again is reuse, whatever state the session is in (FR-007).</summary>
    public static RefreshCheck Evaluate(RefreshToken token, Session session, DateTime now)
    {
        if (token.UsedAt is not null)
            return RefreshCheck.Reused;

        if (token.ExpiresAt <= now || !session.IsUsable(now))
            return RefreshCheck.Invalid;

        return RefreshCheck.Valid;
    }

    /// <summary>256 random bits, URL-safe.</summary>
    public static string NewToken() => WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));

    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
