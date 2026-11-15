using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ModuPOS.Auth.Tokens;

public static class AuthClaims
{
    public const string Subject = "sub";
    public const string Session = "sid";
    public const string Profile = "pid";
    public const string ProfileType = "ptype";
    public const string Business = "bid";
    public const string Branch = "brid";
    public const string AuthorizationVersion = "av";
}

/// <summary>Holds the ES256 signing key shared by token issuing and JwtBearer validation.</summary>
public sealed class JwtKeyProvider
{
    public JwtKeyProvider(IOptions<JwtOptions> options, ILogger<JwtKeyProvider> logger)
    {
        var o = options.Value;
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        if (!string.IsNullOrWhiteSpace(o.PrivateKeyPem))
        {
            ecdsa.ImportFromPem(o.PrivateKeyPem);
        }
        else if (o.AllowEphemeralKey)
        {
            logger.LogWarning("Jwt:PrivateKeyPem is empty; using an ephemeral signing key. Tokens stop working on restart.");
        }
        else
        {
            throw new InvalidOperationException("Jwt:PrivateKeyPem is not configured.");
        }

        Key = new ECDsaSecurityKey(ecdsa) { KeyId = "modupos-access" };
        Credentials = new SigningCredentials(Key, SecurityAlgorithms.EcdsaSha256);
    }

    public ECDsaSecurityKey Key { get; }

    public SigningCredentials Credentials { get; }
}

public sealed record AccessTokenRequest(
    Guid UserId,
    Guid SessionId,
    Guid? ProfileId,
    string? ProfileType,
    Guid? BusinessId,
    Guid? BranchId,
    long AuthorizationVersion);

public sealed record IssuedAccessToken(string Token, DateTime ExpiresAt);

public sealed class JwtTokenService(
    JwtKeyProvider keys,
    IOptions<JwtOptions> options,
    TimeProvider time)
{
    private readonly JsonWebTokenHandler _handler = new();

    public IssuedAccessToken Issue(AccessTokenRequest request)
    {
        var o = options.Value;
        var now = time.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(o.AccessTokenMinutes);

        var claims = new List<Claim>
        {
            new(AuthClaims.Subject, request.UserId.ToString()),
            new(AuthClaims.Session, request.SessionId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(AuthClaims.AuthorizationVersion, request.AuthorizationVersion.ToString(), ClaimValueTypes.Integer64)
        };

        if (request.ProfileId is { } pid) claims.Add(new(AuthClaims.Profile, pid.ToString()));
        if (request.ProfileType is { } ptype) claims.Add(new(AuthClaims.ProfileType, ptype));
        if (request.BusinessId is { } bid) claims.Add(new(AuthClaims.Business, bid.ToString()));
        if (request.BranchId is { } brid) claims.Add(new(AuthClaims.Branch, brid.ToString()));

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = o.Issuer,
            Audience = o.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = keys.Credentials
        });

        return new IssuedAccessToken(token, expires);
    }

    public TokenValidationParameters ValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Value.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Value.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = keys.Key,
        ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
}
