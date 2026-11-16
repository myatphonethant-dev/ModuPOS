using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ModuPOS.Auth.DTOs;
using ModuPOS.Auth.Services.Auth;
using ModuPOS.Database.AppDbContextModels;

namespace ModuPOS.Auth.UnitTests;

public class AuthServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfig()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Jwt:Secret", "ModuPOS_Super_Secret_Key_For_Jwt_Signing_Must_Be_Long_Enough_2026" },
            { "Jwt:Issuer", "ModuPOS" },
            { "Jwt:Audience", "ModuPOS.Api" },
            { "Jwt:AccessTokenMinutes", "60" },
            { "Jwt:RefreshTokenDays", "7" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccessAndTokens()
    {
        // Arrange
        await using var context = CreateContext();
        var config = CreateConfig();
        var hasher = new PasswordHasher<User>();

        var role = new Role { Id = 1, Name = "Admin", IsDeleted = false };
        context.Roles.Add(role);

        var user = new User
        {
            Id = 1,
            Username = "admin",
            RoleId = 1,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, "Password123!");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthService(context, config);

        // Act
        var result = await service.LoginAsync(new LoginRequest("admin", "Password123!"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEmpty(result.Data.AccessToken);
        Assert.NotEmpty(result.Data.RefreshToken);
        Assert.Equal("admin", result.Data.Username);
        Assert.Equal(1, result.Data.UserId);
    }

    [Fact]
    public async Task LoginAsync_InvalidPassword_ReturnsFailure()
    {
        // Arrange
        await using var context = CreateContext();
        var config = CreateConfig();
        var hasher = new PasswordHasher<User>();

        var user = new User
        {
            Id = 1,
            Username = "admin",
            RoleId = 1,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, "CorrectPassword!");
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var service = new AuthService(context, config);

        // Act
        var result = await service.LoginAsync(new LoginRequest("admin", "WrongPassword!"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid username or password.", result.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_ValidToken_RotatesTokenAndSucceeds()
    {
        // Arrange
        await using var context = CreateContext();
        var config = CreateConfig();

        var user = new User
        {
            Id = 1,
            Username = "staff1",
            RoleId = 2,
            PasswordHash = "hash",
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);

        var existingToken = new RefreshToken
        {
            Id = 1,
            UserId = 1,
            RefreshToken1 = "test-refresh-token-123",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        context.RefreshTokens.Add(existingToken);
        await context.SaveChangesAsync();

        var service = new AuthService(context, config);

        // Act
        var result = await service.RefreshTokenAsync(new RefreshTokenRequest("test-refresh-token-123"));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.NotEqual("test-refresh-token-123", result.Data.RefreshToken);

        // Check old token revoked
        var oldToken = await context.RefreshTokens.FindAsync(1);
        Assert.NotNull(oldToken?.RevokedAt);
    }
}
