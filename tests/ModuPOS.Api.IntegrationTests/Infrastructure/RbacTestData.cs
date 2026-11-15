using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Auth.Roles;
using ModuPOS.Auth.Sessions;
using ModuPOS.Organization.Entities;

namespace ModuPOS.Api.IntegrationTests.Infrastructure;

public sealed record OwnerSetup(TestAccount Owner, Guid BusinessId, Guid OwnerRoleId, Guid ManagerRoleId, Guid CashierRoleId);

/// <summary>Helpers for role and assignment tests.</summary>
public static class RbacTestData
{
    /// <summary>A new business with default roles and an Owner staff member.</summary>
    public static async Task<OwnerSetup> CreateBusinessWithOwnerAsync(ApiFactory api)
    {
        var owner = await TestData.CreateStaffAsync(api);

        await using var scope = api.Services.CreateAsyncScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<DefaultRolesProvisioner>();
        var roles = await provisioner.ProvisionAsync(owner.BusinessId, owner.StaffProfileId!.Value, CancellationToken.None);

        return new OwnerSetup(owner, owner.BusinessId, roles.OwnerRoleId, roles.ManagerRoleId, roles.CashierRoleId);
    }

    public static Task<TestAccount> AddStaffAsync(ApiFactory api, Guid businessId) =>
        TestData.CreateStaffAsync(api, businessId: businessId);

    public static Task<Guid> AddBranchAsync(ApiFactory api, Guid businessId, BranchStatus status = BranchStatus.Active) =>
        api.WithDbAsync(async db =>
        {
            var branch = new Branch { BusinessId = businessId, Name = $"Branch {Guid.NewGuid():N}"[..20], Status = status };
            db.Add(branch);
            await db.SaveChangesAsync();
            return branch.Id;
        });

    public static Task AssignBranchAsync(ApiFactory api, TestAccount staff, Guid branchId) =>
        api.WithDbAsync(async db =>
        {
            db.Add(new StaffBranchAssignment { BusinessId = staff.BusinessId, StaffProfileId = staff.StaffProfileId!.Value, BranchId = branchId });
            await db.SaveChangesAsync();
        });

    /// <summary>Gives a role directly in the database (bypasses the API and its checks).</summary>
    public static Task GrantInDbAsync(ApiFactory api, TestAccount staff, Guid roleId, Guid? branchId = null) =>
        api.WithDbAsync(async db =>
        {
            db.Add(new RoleAssignment { BusinessId = staff.BusinessId, StaffProfileId = staff.StaffProfileId!.Value, RoleId = roleId, BranchId = branchId });
            await db.SaveChangesAsync();
        });

    /// <summary>
    /// Puts the staff member's session into a branch and renews to get a token carrying it.
    /// Stand-in for branch selection, which arrives with user story 5.
    /// </summary>
    public static async Task<SignInBody> WithActiveBranchAsync(ApiFactory api, HttpClient client, TestAccount staff, SignInBody tokens, Guid? branchId)
    {
        await api.WithDbAsync(async db =>
        {
            var session = await db.Set<Session>()
                .Where(s => s.UserAccountId == staff.UserId && s.Status == SessionStatus.Active)
                .OrderByDescending(s => s.CreatedAt)
                .FirstAsync();
            session.ActiveBranchId = branchId;
            await db.SaveChangesAsync();
        });

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SignInBody>())!;
    }

    public static async Task<string[]> PermissionsAsync(HttpClient client, string accessToken)
    {
        var me = await client.SendAsync(TestData.Authorized(HttpMethod.Get, "/api/auth/me", accessToken));
        me.EnsureSuccessStatusCode();
        var json = await me.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("permissions").EnumerateArray().Select(e => e.GetString()!).ToArray();
    }

    public static async Task<JsonElement> CreateRoleAsync(HttpClient client, string accessToken, string name, params string[] permissions)
    {
        var response = await client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/roles", accessToken, new { name, permissions }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    /// <summary>Retries until the condition holds or the timeout (default 5 s, SC-003) passes.</summary>
    public static async Task<bool> EventuallyAsync(Func<Task<bool>> condition, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (true)
        {
            if (await condition()) return true;
            if (DateTime.UtcNow > deadline) return false;
            await Task.Delay(200);
        }
    }
}
