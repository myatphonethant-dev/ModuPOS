namespace ModuPOS.Auth.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ModuPOS.Auth.DTOs;
using ModuPOS.Auth.Interfaces;
using ModuPOS.Database.AppDbContextModels;
using ModuPOS.Shared.Results;
using UserEntity = ModuPOS.Database.AppDbContextModels.User;

public class AuthService(AppDbContext context, IConfiguration configuration) : IAuthService
{
    private readonly PasswordHasher<UserEntity> _passwordHasher = new();

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return Result<LoginResponse>.Failure("Username and password are required.");

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && !u.IsDeleted, cancellationToken);

        if (user is null || !user.IsActive)
            return Result<LoginResponse>.Failure("Invalid username or password.");

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
            return Result<LoginResponse>.Failure("Invalid username or password.");

        return await GenerateTokenPairAsync(user, cancellationToken);
    }

    public async Task<Result<LoginResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return Result<LoginResponse>.Failure("Refresh token is required.");

        var tokenRecord = await context.RefreshTokens
            .FirstOrDefaultAsync(r => r.RefreshToken1 == request.RefreshToken && r.RevokedAt == null, cancellationToken);

        if (tokenRecord is null || tokenRecord.ExpiresAt <= DateTime.UtcNow)
            return Result<LoginResponse>.Failure("Invalid or expired refresh token.");

        tokenRecord.RevokedAt = DateTime.UtcNow;

        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Id == tokenRecord.UserId && !u.IsDeleted && u.IsActive, cancellationToken);

        if (user is null)
            return Result<LoginResponse>.Failure("User account not found or inactive.");

        return await GenerateTokenPairAsync(user, cancellationToken);
    }

    public async Task<Result> RevokeTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var tokenRecord = await context.RefreshTokens
            .FirstOrDefaultAsync(r => r.RefreshToken1 == refreshToken && r.RevokedAt == null, cancellationToken);

        if (tokenRecord is null)
            return Result.Failure("Token not found or already revoked.");

        tokenRecord.RevokedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);

        return Result.Success("Token revoked successfully.");
    }

    private async Task<Result<LoginResponse>> GenerateTokenPairAsync(UserEntity user, CancellationToken cancellationToken)
    {
        var role = await context.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == user.RoleId && !r.IsDeleted, cancellationToken);

        var permissions = await (
            from rp in context.RolePermissions
            join p in context.Permissions on rp.PermissionId equals p.Id
            where rp.RoleId == user.RoleId && !rp.IsDeleted && !p.IsDeleted
            select p.Name
        ).ToListAsync(cancellationToken);

        var jwtSecret = configuration["Jwt:Secret"] ?? "ModuPOS_Super_Secret_Key_For_Jwt_Signing_Must_Be_Long_Enough_2026";
        var issuer = configuration["Jwt:Issuer"] ?? "ModuPOS";
        var audience = configuration["Jwt:Audience"] ?? "ModuPOS.Api";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresMinutes = configuration.GetValue<int?>("Jwt:AccessTokenMinutes") ?? 60;
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new("roleId", user.RoleId.ToString()),
            new(ClaimTypes.Role, role?.Name ?? "User")
        };

        foreach (var perm in permissions)
        {
            claims.Add(new Claim("permission", perm));
        }

        var jwtToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds);

        var accessToken = new JwtSecurityTokenHandler().WriteToken(jwtToken);

        var refreshTokenString = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var refreshDays = configuration.GetValue<int?>("Jwt:RefreshTokenDays") ?? 7;
        var refreshExpires = DateTime.UtcNow.AddDays(refreshDays);

        var newRefreshToken = new RefreshToken
        {
            UserId = user.Id,
            RefreshToken1 = refreshTokenString,
            ExpiresAt = refreshExpires,
            CreatedAt = DateTime.UtcNow
        };

        context.RefreshTokens.Add(newRefreshToken);
        await context.SaveChangesAsync(cancellationToken);

        return Result<LoginResponse>.Success(new LoginResponse(
            accessToken,
            refreshTokenString,
            expiresAt,
            user.Id,
            user.Username,
            user.RoleId,
            role?.Name,
            permissions));
    }
}
