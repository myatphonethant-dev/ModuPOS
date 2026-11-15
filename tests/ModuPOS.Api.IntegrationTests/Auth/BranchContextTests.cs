using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ModuPOS.Api.IntegrationTests.Infrastructure;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Organization.Entities;

namespace ModuPOS.Api.IntegrationTests.Auth;

/// <summary>User Story 5: staff branch selection and active branch context.</summary>
[Collection(ApiCollection.Name)]
public class BranchContextTests(ApiFactory api)
{
    private readonly HttpClient _client = api.CreateClient();

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string token, object? body = null) =>
        _client.SendAsync(TestData.Authorized(method, url, token, body));

    private async Task<JsonElement> Branches(string token)
    {
        var response = await Send(HttpMethod.Get, "/api/auth/branches", token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private Task<HttpResponseMessage> Select(string token, Guid branchId) =>
        Send(HttpMethod.Post, "/api/auth/branch", token, new { branchId });

    private static string Token(JsonElement body) => body.GetProperty("accessToken").GetString()!;

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<JsonElement> MeAsync(string token) => await Json(await Send(HttpMethod.Get, "/api/auth/me", token));

    // Scenario 1
    [Fact]
    public async Task Staff_without_active_branches_has_no_branch_access()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var inactive = await RbacTestData.AddBranchAsync(api, setup.BusinessId, BranchStatus.Inactive);
        await RbacTestData.AssignBranchAsync(api, staff, inactive);
        var tokens = await TestData.SignInOkAsync(_client, staff);

        var body = await Branches(tokens.AccessToken);

        Assert.Equal("no_branch_access", body.GetProperty("next").GetString());
        Assert.Empty(body.GetProperty("branches").EnumerateArray());
    }

    // Scenario 2
    [Fact]
    public async Task Single_active_branch_is_selected_automatically()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var only = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        await RbacTestData.AssignBranchAsync(api, staff, only);
        var tokens = await TestData.SignInOkAsync(_client, staff);

        var body = await Branches(tokens.AccessToken);

        Assert.Equal("branch_selected", body.GetProperty("next").GetString());
        var me = await MeAsync(Token(body));
        Assert.Equal(only, me.GetProperty("activeBranch").GetProperty("branchId").GetGuid());

        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, staff.UserId, AuditCodes.BranchSelected));
        Assert.Equal(only, audit.BranchId);
        Assert.Contains($"\"newBranchId\":\"{only}\"", audit.Details);
        Assert.Contains("\"previousBranchId\":null", audit.Details);
    }

    // Scenario 3
    [Fact]
    public async Task Several_branches_offer_only_active_assigned_ones()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var a = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        var b = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        var inactive = await RbacTestData.AddBranchAsync(api, setup.BusinessId, BranchStatus.Inactive);
        await RbacTestData.AddBranchAsync(api, setup.BusinessId); // not assigned
        foreach (var branch in new[] { a, b, inactive })
            await RbacTestData.AssignBranchAsync(api, staff, branch);
        var tokens = await TestData.SignInOkAsync(_client, staff);

        var body = await Branches(tokens.AccessToken);

        Assert.Equal("choose_branch", body.GetProperty("next").GetString());
        var ids = body.GetProperty("branches").EnumerateArray().Select(x => x.GetProperty("branchId").GetGuid()).ToHashSet();
        Assert.Equal(new HashSet<Guid> { a, b }, ids);
        Assert.Equal(JsonValueKind.Null, (await MeAsync(tokens.AccessToken)).GetProperty("activeBranch").ValueKind);
    }

    // Scenario 4
    [Fact]
    public async Task Unassigned_inactive_or_foreign_branch_is_refused_with_its_reason()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var unassigned = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        var inactive = await RbacTestData.AddBranchAsync(api, setup.BusinessId, BranchStatus.Inactive);
        await RbacTestData.AssignBranchAsync(api, staff, inactive);
        var other = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var foreign = await RbacTestData.AddBranchAsync(api, other.BusinessId);
        var tokens = await TestData.SignInOkAsync(_client, staff);

        foreach (var (branch, reason) in new[] { (unassigned, "not_assigned"), (inactive, "inactive"), (foreign, "other_business") })
        {
            var response = await Select(tokens.AccessToken, branch);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            var problem = await Json(response);
            Assert.Equal("branch_denied", problem.GetProperty("code").GetString());
            Assert.Equal(reason, problem.GetProperty("reason").GetString());
        }

        Assert.Equal(3, (await TestData.WaitForAuditAsync(api, staff.UserId, AuditCodes.BranchDenied, expectedAtLeast: 3)).Count);
    }

    // Scenario 5
    [Fact]
    public async Task Switching_branch_recalculates_permissions_without_signing_in_again()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var a = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        var b = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        await RbacTestData.AssignBranchAsync(api, staff, a);
        await RbacTestData.AssignBranchAsync(api, staff, b);
        await RbacTestData.GrantInDbAsync(api, staff, setup.ManagerRoleId, a);
        var tokens = await TestData.SignInOkAsync(_client, staff);

        var inA = Token(await Json(await Select(tokens.AccessToken, a)));
        Assert.Contains("role.assign", await RbacTestData.PermissionsAsync(_client, inA));

        var inB = Token(await Json(await Select(inA, b)));
        Assert.Empty(await RbacTestData.PermissionsAsync(_client, inB));

        var switches = await TestData.WaitForAuditAsync(api, staff.UserId, AuditCodes.BranchSelected, expectedAtLeast: 2);
        Assert.Contains(switches, s => s.Details.Contains($"\"previousBranchId\":\"{a}\"") && s.Details.Contains($"\"newBranchId\":\"{b}\""));

        // Renewal keeps the active branch.
        var renewed = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });
        var renewedToken = (await renewed.Content.ReadFromJsonAsync<SignInBody>())!.AccessToken;
        Assert.Equal(b, (await MeAsync(renewedToken)).GetProperty("activeBranch").GetProperty("branchId").GetGuid());
    }

    [Fact]
    public async Task Branch_selection_needs_the_staff_portal()
    {
        var account = await TestData.CreateStaffAsync(api, withStaffProfile: false);
        await api.WithDbAsync(async db =>
        {
            db.Add(new Profile { UserAccountId = account.UserId, Type = ProfileType.Member, BusinessId = account.BusinessId });
            await db.SaveChangesAsync();
        });
        var tokens = await TestData.SignInOkAsync(_client, account);

        var response = await Send(HttpMethod.Get, "/api/auth/branches", tokens.AccessToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("staff_portal_required", (await response.Content.ReadFromJsonAsync<ProblemBody>())!.Code);
    }
}
