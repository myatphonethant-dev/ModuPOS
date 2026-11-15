using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Tokens;

namespace ModuPOS.Auth.Sessions;

/// <summary>Where a request came from; stored on sessions and audit entries.</summary>
public sealed record RequestInfo(string? SourceAddress, string? DeviceDescription);

public sealed record TokenPair(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt);

/// <summary>Creates renewal credentials and access tokens for a session.</summary>
public sealed class SessionTokens(
    DbContext db,
    JwtTokenService jwt,
    AuthorizationVersionService versions,
    IOptions<JwtOptions> options,
    TimeProvider time)
{
    /// <summary>Adds a new renewal credential for the session (not yet saved) and returns its raw value.</summary>
    public (RefreshToken Row, string Raw) NewRefreshToken(Session session)
    {
        var now = time.GetUtcNow().UtcDateTime;
        var raw = RefreshTokenEvaluator.NewToken();
        var row = new RefreshToken
        {
            SessionId = session.Id,
            TokenHash = RefreshTokenEvaluator.Hash(raw),
            IssuedAt = now,
            ExpiresAt = now.AddDays(options.Value.RefreshTokenDays)
        };

        db.Set<RefreshToken>().Add(row);
        return (row, raw);
    }

    public async Task<IssuedAccessToken> IssueAccessTokenAsync(Session session, CancellationToken ct)
    {
        Profile? profile = session.ActiveProfileId is { } pid
            ? await db.Set<Profile>().AsNoTracking().FirstOrDefaultAsync(p => p.Id == pid, ct)
            : null;

        var version = profile?.BusinessId is { } bid ? await versions.GetAsync(bid, ct) : 0;

        return jwt.Issue(new AccessTokenRequest(
            session.UserAccountId,
            session.Id,
            profile?.Id,
            profile?.Type.ToString(),
            profile?.BusinessId,
            profile?.Type == ProfileType.Staff ? session.ActiveBranchId : null,
            version));
    }
}
