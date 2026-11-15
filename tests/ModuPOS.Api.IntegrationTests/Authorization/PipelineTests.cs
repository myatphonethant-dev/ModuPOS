using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Api.IntegrationTests.Infrastructure;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Sessions;
using ModuPOS.Organization.Entities;
using ModuPOS.Shared.Auditing;

namespace ModuPOS.Api.IntegrationTests.Authorization;

/// <summary>User Story 6: every protected action is checked the same way, in order.</summary>
[Collection(ApiCollection.Name)]
public class PipelineTests(ApiFactory api)
{
    private readonly HttpClient _client = api.CreateClient();

    private Task<HttpResponseMessage> Get(string url, string? token) =>
        token is null ? _client.GetAsync(url) : _client.SendAsync(TestData.Authorized(HttpMethod.Get, url, token));

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task AssertProblem(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.Equal(code, (await Json(response)).GetProperty("code").GetString());
    }

    /// <summary>A manager (role.view etc.) working in an active branch of a fresh business.</summary>
    private async Task<(OwnerSetup Setup, TestAccount Staff, Guid Branch, SignInBody Tokens)> ManagerInBranchAsync()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var branch = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        await RbacTestData.AssignBranchAsync(api, staff, branch);
        await RbacTestData.GrantInDbAsync(api, staff, setup.ManagerRoleId);
        var tokens = await TestData.SignInOkAsync(_client, staff);
        var select = await _client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/auth/branch", tokens.AccessToken, new { branchId = branch }));
        var access = (await Json(select)).GetProperty("accessToken").GetString()!;
        return (setup, staff, branch, tokens with { AccessToken = access });
    }

    [Fact]
    public async Task All_checks_pass_for_a_valid_request()
    {
        var (_, _, branch, tokens) = await ManagerInBranchAsync();

        var response = await Get("/test/branch-action", tokens.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(branch, (await Json(response)).GetProperty("branchId").GetGuid());
    }

    // Step 1
    [Fact]
    public async Task Step1_missing_or_garbage_token_is_not_signed_in()
    {
        await AssertProblem(await Get("/test/branch-action", null), HttpStatusCode.Unauthorized, "not_signed_in");
        await AssertProblem(await Get("/test/branch-action", "not.a.jwt"), HttpStatusCode.Unauthorized, "not_signed_in");
    }

    // Step 2 + edge case: business suspended while signed in
    [Fact]
    public async Task Step2_suspended_business_is_tenant_invalid_and_sessions_end()
    {
        var (setup, staff, _, tokens) = await ManagerInBranchAsync();
        await api.WithDbAsync(async db =>
        {
            (await db.Set<Business>().FindAsync(setup.BusinessId))!.Status = BusinessStatus.Suspended;
            await db.SaveChangesAsync();
        });
        await api.BumpVersionAsync(setup.BusinessId);

        await AssertProblem(await Get("/api/roles", tokens.AccessToken), HttpStatusCode.Forbidden, "tenant_invalid");
        await AssertProblem(await Get("/api/roles", tokens.AccessToken), HttpStatusCode.Unauthorized, "not_signed_in");

        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, staff.UserId, AuditCodes.SessionsRevoked));
        Assert.Contains("business_suspended", audit.Details);
    }

    // Step 3 + edge case: account suspended while signed in
    [Fact]
    public async Task Step3_suspended_account_is_account_inactive_and_sessions_end()
    {
        var (setup, staff, _, tokens) = await ManagerInBranchAsync();
        await api.WithDbAsync(async db =>
        {
            (await db.Set<UserAccount>().FindAsync(staff.UserId))!.Status = AccountStatus.Suspended;
            await db.SaveChangesAsync();
        });
        await api.BumpVersionAsync(setup.BusinessId);

        await AssertProblem(await Get("/api/roles", tokens.AccessToken), HttpStatusCode.Forbidden, "account_inactive");
        await AssertProblem(await Get("/api/roles", tokens.AccessToken), HttpStatusCode.Unauthorized, "not_signed_in");
        Assert.Contains("account_suspended", Assert.Single(await TestData.WaitForAuditAsync(api, staff.UserId, AuditCodes.SessionsRevoked)).Details);
    }

    [Fact]
    public async Task Step3_inactive_staff_profile_is_account_inactive()
    {
        var (setup, staff, _, tokens) = await ManagerInBranchAsync();
        await api.WithDbAsync(async db =>
        {
            (await db.Set<Profile>().FindAsync(staff.StaffProfileId))!.Status = ProfileStatus.Inactive;
            await db.SaveChangesAsync();
        });
        await api.BumpVersionAsync(setup.BusinessId);

        await AssertProblem(await Get("/api/roles", tokens.AccessToken), HttpStatusCode.Forbidden, "account_inactive");
    }

    // Step 4
    [Fact]
    public async Task Step4_invalid_license_is_refused_before_permission_checks()
    {
        var (setup, _, _, tokens) = await ManagerInBranchAsync();
        var cashier = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, cashier, setup.CashierRoleId);
        var cashierTokens = await TestData.SignInOkAsync(_client, cashier);

        api.Licenses.Invalidate(setup.BusinessId);
        try
        {
            await AssertProblem(await Get("/api/roles", tokens.AccessToken), HttpStatusCode.Forbidden, "license_invalid");
            // The cashier lacks role.view too, but the earlier step wins.
            await AssertProblem(await Get("/api/roles", cashierTokens.AccessToken), HttpStatusCode.Forbidden, "license_invalid");
        }
        finally
        {
            api.Licenses.Restore(setup.BusinessId);
        }

        Assert.Equal(HttpStatusCode.OK, (await Get("/api/roles", tokens.AccessToken)).StatusCode);
    }

    // Step 5
    [Fact]
    public async Task Step5_disabled_feature_is_feature_unavailable()
    {
        var (setup, _, _, tokens) = await ManagerInBranchAsync();

        await api.DisableFeatureAsync(setup.BusinessId, "POS.AUTH");
        try
        {
            await AssertProblem(await Get("/api/roles", tokens.AccessToken), HttpStatusCode.Forbidden, "feature_unavailable");
        }
        finally
        {
            await api.EnableFeatureAsync(setup.BusinessId, "POS.AUTH");
        }
    }

    // Step 6 + FR-038 dedup
    [Fact]
    public async Task Step6_missing_permission_is_denied_and_repeated_refusals_collapse_into_one_entry()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var cashier = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, cashier, setup.CashierRoleId);
        var tokens = await TestData.SignInOkAsync(_client, cashier);

        for (var i = 0; i < 3; i++)
            await AssertProblem(await Get("/api/roles", tokens.AccessToken), HttpStatusCode.Forbidden, "permission_denied");

        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, cashier.UserId, AuditCodes.PermissionDenied));
        Assert.Equal(3, audit.RepeatCount);
        Assert.Contains("role.view", audit.Details);
    }

    // Step 7
    [Fact]
    public async Task Step7_branch_action_without_an_active_branch_is_denied()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var owner = await TestData.SignInOkAsync(_client, setup.Owner);

        var response = await Get("/test/branch-action", owner.AccessToken);

        await AssertProblem(response, HttpStatusCode.Forbidden, "branch_denied");
        Assert.Equal("no_active_branch", (await Json(await Get("/test/branch-action", owner.AccessToken))).GetProperty("reason").GetString());
    }

    // Step 7 + edge case: branch deactivated mid-shift
    [Fact]
    public async Task Step7_branch_deactivated_while_working_is_denied_on_the_next_action()
    {
        var (setup, staff, branch, tokens) = await ManagerInBranchAsync();
        Assert.Equal(HttpStatusCode.OK, (await Get("/test/branch-action", tokens.AccessToken)).StatusCode);

        await api.WithDbAsync(async db =>
        {
            (await db.Set<Branch>().IgnoreQueryFilters().SingleAsync(b => b.Id == branch)).Status = BranchStatus.Inactive;
            await db.SaveChangesAsync();
        });
        await api.BumpVersionAsync(setup.BusinessId);

        var response = await Get("/test/branch-action", tokens.AccessToken);
        await AssertProblem(response, HttpStatusCode.Forbidden, "branch_denied");
        Assert.Equal("inactive", (await Json(await Get("/test/branch-action", tokens.AccessToken))).GetProperty("reason").GetString());

        // Non-branch actions still work.
        Assert.Equal(HttpStatusCode.OK, (await Get("/api/roles", tokens.AccessToken)).StatusCode);
        Assert.NotEmpty(await TestData.WaitForAuditAsync(api, staff.UserId, AuditCodes.BranchDenied));
    }

    [Fact]
    public async Task Step7_removed_from_the_branch_is_denied()
    {
        var (setup, staff, branch, tokens) = await ManagerInBranchAsync();
        await api.WithDbAsync(async db =>
        {
            db.RemoveRange(db.Set<StaffBranchAssignment>().IgnoreQueryFilters().Where(s => s.StaffProfileId == staff.StaffProfileId && s.BranchId == branch));
            await db.SaveChangesAsync();
        });
        await api.BumpVersionAsync(setup.BusinessId);

        var response = await Get("/test/branch-action", tokens.AccessToken);

        await AssertProblem(response, HttpStatusCode.Forbidden, "branch_denied");
    }

    // FR-021, SC-004
    [Fact]
    public async Task Cross_tenant_role_access_looks_like_not_found_and_is_audited_high()
    {
        var a = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var b = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var ownerA = await TestData.SignInOkAsync(_client, a.Owner);

        var response = await Get($"/api/roles/{b.ManagerRoleId}", ownerA.AccessToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, a.Owner.UserId, AuditCodes.CrossTenantDenied));
        Assert.Equal(AuditSeverity.High, audit.Severity);
        Assert.Contains(b.BusinessId.ToString(), audit.Details);
    }

    [Fact]
    public async Task Cross_tenant_business_id_in_the_route_is_refused()
    {
        var a = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var b = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var ownerA = await TestData.SignInOkAsync(_client, a.Owner);

        Assert.Equal(HttpStatusCode.OK, (await Get($"/test/businesses/{a.BusinessId}/thing", ownerA.AccessToken)).StatusCode);
        var response = await Get($"/test/businesses/{b.BusinessId}/thing", ownerA.AccessToken);

        await AssertProblem(response, HttpStatusCode.NotFound, "not_found");
        Assert.Single(await TestData.WaitForAuditAsync(api, a.Owner.UserId, AuditCodes.CrossTenantDenied));
    }

    [Fact]
    public async Task Cross_tenant_staff_assignment_is_audited()
    {
        var a = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var b = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var ownerA = await TestData.SignInOkAsync(_client, a.Owner);

        var response = await _client.SendAsync(TestData.Authorized(HttpMethod.Get,
            $"/api/staff/{b.Owner.StaffProfileId}/role-assignments", ownerA.AccessToken));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Single(await TestData.WaitForAuditAsync(api, a.Owner.UserId, AuditCodes.CrossTenantDenied));
    }

    // FR-032 + T057
    [Fact]
    public async Task Platform_admin_uses_platform_permissions_only()
    {
        var admin = await TestData.CreateStaffAsync(api, withStaffProfile: false);
        await api.WithDbAsync(async db =>
        {
            db.Add(new Profile { UserAccountId = admin.UserId, Type = ProfileType.PlatformAdmin });
            await db.SaveChangesAsync();
        });
        var adminTokens = await TestData.SignInOkAsync(_client, admin);
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var owner = await TestData.SignInOkAsync(_client, setup.Owner);

        Assert.Equal(HttpStatusCode.OK, (await Get("/test/platform-action", adminTokens.AccessToken)).StatusCode);
        await AssertProblem(await Get("/api/roles", adminTokens.AccessToken), HttpStatusCode.Forbidden, "permission_denied");
        await AssertProblem(await Get("/test/platform-action", owner.AccessToken), HttpStatusCode.Forbidden, "permission_denied");
    }

    [Fact]
    public async Task Suspended_platform_admin_is_refused()
    {
        var admin = await TestData.CreateStaffAsync(api, withStaffProfile: false);
        await api.WithDbAsync(async db =>
        {
            db.Add(new Profile { UserAccountId = admin.UserId, Type = ProfileType.PlatformAdmin });
            await db.SaveChangesAsync();
        });
        var tokens = await TestData.SignInOkAsync(_client, admin);
        Assert.Equal(HttpStatusCode.OK, (await Get("/test/platform-action", tokens.AccessToken)).StatusCode);

        await api.WithDbAsync(async db =>
        {
            (await db.Set<UserAccount>().FindAsync(admin.UserId))!.Status = AccountStatus.Suspended;
            await db.SaveChangesAsync();
        });

        Assert.True(await RbacTestData.EventuallyAsync(async () =>
            (await Get("/test/platform-action", tokens.AccessToken)).StatusCode != HttpStatusCode.OK));
    }

    // T058: revocation when an admin suspends an account or business
    [Fact]
    public async Task Suspending_an_account_ends_all_its_sessions_at_once()
    {
        var (_, staff, _, tokens) = await ManagerInBranchAsync();
        var second = await TestData.SignInOkAsync(_client, staff);

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var ended = await scope.ServiceProvider.GetRequiredService<SessionRevocationService>()
                .SetAccountStatusAsync(staff.UserId, AccountStatus.Suspended, CancellationToken.None);
            Assert.Equal(2, ended);
        }

        await AssertProblem(await Get("/api/auth/me", tokens.AccessToken), HttpStatusCode.Unauthorized, "not_signed_in");
        await AssertProblem(await Get("/api/auth/me", second.AccessToken), HttpStatusCode.Unauthorized, "not_signed_in");
        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, null, AuditCodes.SessionsRevoked, targetId: staff.UserId.ToString()));
        Assert.Contains("\"sessionsEnded\":2", audit.Details);
    }

    [Fact]
    public async Task Suspending_a_business_ends_the_sessions_working_in_it()
    {
        var (setup, staff, _, tokens) = await ManagerInBranchAsync();
        var owner = await TestData.SignInOkAsync(_client, setup.Owner);
        var outsider = await TestData.CreateStaffAsync(api);
        var outsiderTokens = await TestData.SignInOkAsync(_client, outsider);

        await using (var scope = api.Services.CreateAsyncScope())
        {
            var ended = await scope.ServiceProvider.GetRequiredService<SessionRevocationService>()
                .SetBusinessStatusAsync(setup.BusinessId, BusinessStatus.Suspended, CancellationToken.None);
            Assert.Equal(2, ended);
        }

        await AssertProblem(await Get("/api/auth/me", tokens.AccessToken), HttpStatusCode.Unauthorized, "not_signed_in");
        await AssertProblem(await Get("/api/auth/me", owner.AccessToken), HttpStatusCode.Unauthorized, "not_signed_in");
        Assert.Equal(HttpStatusCode.OK, (await Get("/api/auth/me", outsiderTokens.AccessToken)).StatusCode);

        var sessions = await api.WithDbAsync(db => db.Set<Session>().Where(s => s.UserAccountId == staff.UserId).ToListAsync());
        Assert.All(sessions, s => Assert.Equal(SessionRevokedReason.BusinessSuspended, s.RevokedReason));
    }
}
