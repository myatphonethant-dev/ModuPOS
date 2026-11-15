using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Organization.Entities;

namespace ModuPOS.Api.IntegrationTests.Infrastructure;

public sealed record TestAccount(Guid UserId, string Username, string Email, string Password, Guid BusinessId, Guid? StaffProfileId);

public sealed record SignInBody(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    List<ProfileBody> Profiles,
    string Next);

public sealed record ProfileBody(Guid ProfileId, string Type, Guid? BusinessId, string? BusinessName);

public sealed record ProblemBody(string? Code, string? Title, int? Status);

/// <summary>Builds isolated test data; every call uses unique names so tests can share one database.</summary>
public static class TestData
{
    public const string Password = "Test-Passw0rd!";

    public static async Task<TestAccount> CreateStaffAsync(
        ApiFactory api,
        AccountStatus status = AccountStatus.Active,
        bool withStaffProfile = true,
        Guid? businessId = null)
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var username = $"u{suffix}";
        var email = $"{username}@example.test";

        return await api.WithDbAsync(async db =>
        {
            Business business;
            if (businessId is { } existing)
            {
                business = await db.Set<Business>().SingleAsync(b => b.Id == existing);
            }
            else
            {
                business = new Business { Name = $"Biz {suffix}" };
                db.Add(business);
            }

            var account = new UserAccount { Username = username, Email = email, Status = status };
            account.PasswordHash = new PasswordHasher<UserAccount>().HashPassword(account, Password);
            db.Add(account);
            await db.SaveChangesAsync();

            Profile? profile = null;
            if (withStaffProfile)
            {
                profile = new Profile { UserAccountId = account.Id, Type = ProfileType.Staff, BusinessId = business.Id };
                db.Add(profile);
                await db.SaveChangesAsync();
            }

            return new TestAccount(account.Id, username, email, Password, business.Id, profile?.Id);
        });
    }

    public static Task<HttpResponseMessage> SignInAsync(HttpClient client, string login, string password) =>
        client.PostAsJsonAsync("/api/auth/sign-in", new { login, password });

    public static async Task<SignInBody> SignInOkAsync(HttpClient client, TestAccount account)
    {
        var response = await SignInAsync(client, account.Username, account.Password);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignInBody>())!;
    }

    public static HttpRequestMessage Authorized(HttpMethod method, string url, string accessToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new("Bearer", accessToken);
        if (body is not null) request.Content = JsonContent.Create(body);
        return request;
    }

    /// <summary>Waits for the background audit writer to save an entry matching the filter.</summary>
    public static async Task<List<AuditEntryRecord>> WaitForAuditAsync(
        ApiFactory api,
        Guid? actorUserId,
        string code,
        int expectedAtLeast = 1,
        string? targetId = null)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            var rows = await api.WithDbAsync(db => db.Set<AuditEntryRecord>()
                .IgnoreQueryFilters()
                .Where(a => a.Code == code && a.ActorUserId == actorUserId && (targetId == null || a.TargetId == targetId))
                .ToListAsync());

            if (rows.Count >= expectedAtLeast || DateTime.UtcNow > deadline)
                return rows;

            await Task.Delay(100);
        }
    }
}
