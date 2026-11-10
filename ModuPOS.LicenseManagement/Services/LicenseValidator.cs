using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModuPOS.LicenseManagement.Interfaces;
using ModuPOS.LicenseManagement.Models;

namespace ModuPOS.LicenseManagement.Services;

public sealed class LicenseValidator(
    IOptions<LicenseValidationOptions> options) : ILicenseValidator
{
    private readonly LicenseValidationOptions _options = options.Value;

    public Task<LicenseValidationResult> ValidateAsync(
        string licenseToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(licenseToken))
            return Task.FromResult(
                LicenseValidationResult.Failed("License token is missing."));

        if (string.IsNullOrWhiteSpace(_options.PublicKeyPem) ||
            string.IsNullOrWhiteSpace(_options.ExpectedInstallationId))
        {
            return Task.FromResult(
                LicenseValidationResult.Failed("License validation is not configured."));
        }

        try
        {
            using var ecdsa = ECDsa.Create();

            ecdsa.ImportFromPem(_options.PublicKeyPem);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new ECDsaSecurityKey(ecdsa),

                ValidAlgorithms = [SecurityAlgorithms.EcdsaSha256],

                ValidateIssuer = true,
                ValidIssuer = _options.Issuer,

                ValidateAudience = true,
                ValidAudience = _options.Audience,

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ClockSkew = TimeSpan.FromMinutes(2)
            };

            var handler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };

            var principal = handler.ValidateToken(
                licenseToken,
                validationParameters,
                out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwt ||
                jwt.Header.Alg != SecurityAlgorithms.EcdsaSha256)
            {
                return Task.FromResult(
                    LicenseValidationResult.Failed("Invalid signing algorithm."));
            }

            var licenseId = principal.FindFirst("jti")?.Value;
            var customerIdText = principal.FindFirst("customer_id")?.Value;
            var installationId = principal.FindFirst("installation_id")?.Value;
            var licenseType = principal.FindFirst("license_type")?.Value;

            var features = principal.FindAll("feature")
                .Select(x => x.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (string.IsNullOrWhiteSpace(licenseId) ||
                !Guid.TryParse(customerIdText, out var customerId) ||
                string.IsNullOrWhiteSpace(installationId) ||
                string.IsNullOrWhiteSpace(licenseType) ||
                features.Count == 0)
            {
                return Task.FromResult(
                    LicenseValidationResult.Failed("Required license claims are missing."));
            }

            if (!string.Equals(
                    installationId,
                    _options.ExpectedInstallationId,
                    StringComparison.Ordinal))
            {
                return Task.FromResult(
                    LicenseValidationResult.Failed("License belongs to another installation."));
            }

            var issuedAt = ReadUnixTimeClaim(principal, "iat");
            var expiresAt = ReadUnixTimeClaim(principal, "exp");

            if (issuedAt is null ||
                expiresAt is null ||
                expiresAt <= issuedAt)
            {
                return Task.FromResult(
                    LicenseValidationResult.Failed("Invalid license dates."));
            }

            var payload = new LicensePayload
            {
                LicenseId = licenseId,
                CustomerId = customerId,
                InstallationId = installationId,
                LicenseType = licenseType,
                Features = features,
                IssuedAt = issuedAt.Value,
                ExpiresAt = expiresAt.Value
            };

            return Task.FromResult(
                LicenseValidationResult.Success(payload));
        }
        catch (SecurityTokenException)
        {
            return Task.FromResult(
                LicenseValidationResult.Failed("Invalid or expired license."));
        }
        catch (CryptographicException)
        {
            return Task.FromResult(
                LicenseValidationResult.Failed("Invalid public key."));
        }
        catch (ArgumentException)
        {
            return Task.FromResult(
                LicenseValidationResult.Failed("Malformed license token or public key."));
        }
    }

    private static DateTime? ReadUnixTimeClaim(
        ClaimsPrincipal principal,
        string claimType)
    {
        var value = principal.FindFirst(claimType)?.Value;

        return long.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var seconds)
                ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
                : null;
    }
}