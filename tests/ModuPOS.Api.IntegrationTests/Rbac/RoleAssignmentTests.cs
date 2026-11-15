using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ModuPOS.Api.IntegrationTests.Infrastructure;
using ModuPOS.Auth.Auditing;

namespace ModuPOS.Api.IntegrationTests.Rbac;

/// <summary>User Story 4: assign roles to staff at business or branch scope.</summary>
[Collection(ApiCollection.Name)]
public class RoleAssignmentTests(ApiFactory api)
{
    private readonly HttpClient _client = api.CreateClient();

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string token, object? body = null) =>
        _client.SendAsync(TestData.Authorized(method, url, token, body));

    private static async Task<string?> Code(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<ProblemBody>())!.Code;

    private Task<HttpResponseMessage> Grant(string token, TestAccount staff, Guid roleId, Guid? branchId = null) =>
        Send(HttpMethod.Post, $"/api/staff/{staff.StaffProfileId}/role-assignments", token, new { roleId, branchId });

    private sealed record World(OwnerSetup Setup, SignInBody Owner, Guid BranchA, Guid BranchB, TestAccount Staff);

    /// <summary>A business with branches A and B and a staff member assigned to both.</summary>
    private async Task<World> WorldAsync()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var owner = await TestData.SignInOkAsync(_client, setup.Owner);
        var a = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        var b = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.AssignBranchAsync(api, staff, a);
        await RbacTestData.AssignBranchAsync(api, staff, b);
        return new World(setup, owner, a, b, staff);
    }

    // Scenario 1
    [Fact]
    public async Task Business_scope_role_applies_in_every_branch()
    {
        var w = await WorldAsync();

        var grant = await Grant(w.Owner.AccessToken, w.Staff, w.Setup.ManagerRoleId);
        Assert.Equal(HttpStatusCode.Created, grant.StatusCode);

        var tokens = await TestData.SignInOkAsync(_client, w.Staff);
        var inA = await RbacTestData.WithActiveBranchAsync(api, _client, w.Staff, tokens, w.BranchA);
        Assert.Contains("role.assign", await RbacTestData.PermissionsAsync(_client, inA.AccessToken));
        var inB = await RbacTestData.WithActiveBranchAsync(api, _client, w.Staff, inA, w.BranchB);
        Assert.Contains("role.assign", await RbacTestData.PermissionsAsync(_client, inB.AccessToken));

        var assignmentId = (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, w.Setup.Owner.UserId, AuditCodes.RoleAssignmentGranted, targetId: assignmentId.ToString()));
        Assert.Contains("\"scope\":\"business\"", audit.Details);
        Assert.Contains(w.Staff.StaffProfileId!.Value.ToString(), audit.Details);
    }

    // Scenario 2 and the US4 independent test
    [Fact]
    public async Task Manager_at_branch_A_and_cashier_at_branch_B_get_different_powers()
    {
        var w = await WorldAsync();
        Assert.Equal(HttpStatusCode.Created, (await Grant(w.Owner.AccessToken, w.Staff, w.Setup.ManagerRoleId, w.BranchA)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await Grant(w.Owner.AccessToken, w.Staff, w.Setup.CashierRoleId, w.BranchB)).StatusCode);

        var tokens = await TestData.SignInOkAsync(_client, w.Staff);
        var inA = await RbacTestData.WithActiveBranchAsync(api, _client, w.Staff, tokens, w.BranchA);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/api/roles", inA.AccessToken)).StatusCode);

        var inB = await RbacTestData.WithActiveBranchAsync(api, _client, w.Staff, inA, w.BranchB);
        var refused = await Send(HttpMethod.Get, "/api/roles", inB.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal("permission_denied", await Code(refused));
    }

    // Scenario 3
    [Fact]
    public async Task Business_and_branch_roles_combine_in_the_active_branch()
    {
        var w = await WorldAsync();
        var auditor = await RbacTestData.CreateRoleAsync(_client, w.Owner.AccessToken, "Auditor", "audit.view");
        await Grant(w.Owner.AccessToken, w.Staff, auditor.GetProperty("id").GetGuid());
        await Grant(w.Owner.AccessToken, w.Staff, w.Setup.ManagerRoleId, w.BranchA);

        var tokens = await TestData.SignInOkAsync(_client, w.Staff);
        var inA = await RbacTestData.WithActiveBranchAsync(api, _client, w.Staff, tokens, w.BranchA);
        var inB = await RbacTestData.WithActiveBranchAsync(api, _client, w.Staff, inA, w.BranchB);

        Assert.Equal(["audit.view", "role.assign", "role.view"], await RbacTestData.PermissionsAsync(_client, inA.AccessToken));
        Assert.Equal(["audit.view"], await RbacTestData.PermissionsAsync(_client, inB.AccessToken));
    }

    // Scenario 4
    [Fact]
    public async Task Removing_an_assignment_takes_effect_on_the_next_action()
    {
        var w = await WorldAsync();
        var grant = await Grant(w.Owner.AccessToken, w.Staff, w.Setup.ManagerRoleId);
        var assignmentId = (await grant.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var tokens = await TestData.SignInOkAsync(_client, w.Staff);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/api/roles", tokens.AccessToken)).StatusCode);

        var revoke = await Send(HttpMethod.Delete, $"/api/staff/{w.Staff.StaffProfileId}/role-assignments/{assignmentId}", w.Owner.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        Assert.True(await RbacTestData.EventuallyAsync(async () =>
            (await Send(HttpMethod.Get, "/api/roles", tokens.AccessToken)).StatusCode == HttpStatusCode.Forbidden));
        Assert.Single(await TestData.WaitForAuditAsync(api, w.Setup.Owner.UserId, AuditCodes.RoleAssignmentRevoked, targetId: assignmentId.ToString()));
    }

    // Scenario 5
    [Fact]
    public async Task Branch_assignment_needs_the_staff_member_to_work_at_that_branch()
    {
        var w = await WorldAsync();
        var otherBranch = await RbacTestData.AddBranchAsync(api, w.Setup.BusinessId);

        var response = await Grant(w.Owner.AccessToken, w.Staff, w.Setup.ManagerRoleId, otherBranch);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("branch_not_assigned", await Code(response));
    }

    [Fact]
    public async Task Same_role_at_the_same_scope_twice_is_rejected_but_another_scope_is_fine()
    {
        var w = await WorldAsync();
        Assert.Equal(HttpStatusCode.Created, (await Grant(w.Owner.AccessToken, w.Staff, w.Setup.CashierRoleId)).StatusCode);

        var duplicate = await Grant(w.Owner.AccessToken, w.Staff, w.Setup.CashierRoleId);
        var branchScope = await Grant(w.Owner.AccessToken, w.Staff, w.Setup.CashierRoleId, w.BranchA);

        Assert.Equal("assignment_exists", await Code(duplicate));
        Assert.Equal(HttpStatusCode.Created, branchScope.StatusCode);

        var list = await (await Send(HttpMethod.Get, $"/api/staff/{w.Staff.StaffProfileId}/role-assignments", w.Owner.AccessToken))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, list.GetArrayLength());
    }

    // FR-027 / SC-007
    [Fact]
    public async Task The_last_owner_cannot_be_removed_but_one_of_two_can()
    {
        var w = await WorldAsync();
        var list = await (await Send(HttpMethod.Get, $"/api/staff/{w.Setup.Owner.StaffProfileId}/role-assignments", w.Owner.AccessToken))
            .Content.ReadFromJsonAsync<JsonElement>();
        var ownerAssignment = list.EnumerateArray().Single().GetProperty("id").GetGuid();
        var url = $"/api/staff/{w.Setup.Owner.StaffProfileId}/role-assignments/{ownerAssignment}";

        var blocked = await Send(HttpMethod.Delete, url, w.Owner.AccessToken);

        Assert.Equal(HttpStatusCode.Conflict, blocked.StatusCode);
        Assert.Equal("last_owner", await Code(blocked));
        Assert.Single(await TestData.WaitForAuditAsync(api, w.Setup.Owner.UserId, AuditCodes.OwnerProtectionBlocked, targetId: ownerAssignment.ToString()));

        // With a second Owner, the first can step down.
        Assert.Equal(HttpStatusCode.Created, (await Grant(w.Owner.AccessToken, w.Staff, w.Setup.OwnerRoleId)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Delete, url, w.Owner.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Owner_role_can_only_be_given_for_the_whole_business()
    {
        var w = await WorldAsync();

        var response = await Grant(w.Owner.AccessToken, w.Staff, w.Setup.OwnerRoleId, w.BranchA);

        Assert.Equal("owner_business_scope_only", await Code(response));
    }

    // FR-017: a manager can assign roles but not hand out more than they hold.
    [Fact]
    public async Task Manager_cannot_assign_a_role_with_permissions_they_lack()
    {
        var w = await WorldAsync();
        await Grant(w.Owner.AccessToken, w.Staff, w.Setup.ManagerRoleId);
        var roleMaker = await RbacTestData.CreateRoleAsync(_client, w.Owner.AccessToken, "Role Maker", "role.manage");
        var colleague = await RbacTestData.AddStaffAsync(api, w.Setup.BusinessId);
        var manager = await TestData.SignInOkAsync(_client, w.Staff);

        var escalation = await Grant(manager.AccessToken, colleague, roleMaker.GetProperty("id").GetGuid());
        var makeOwner = await Grant(manager.AccessToken, colleague, w.Setup.OwnerRoleId);
        var fine = await Grant(manager.AccessToken, colleague, w.Setup.CashierRoleId);

        Assert.Equal("escalation_blocked", await Code(escalation));
        Assert.Equal("escalation_blocked", await Code(makeOwner));
        Assert.Equal(HttpStatusCode.Created, fine.StatusCode);
        Assert.Equal(2, (await TestData.WaitForAuditAsync(api, w.Staff.UserId, AuditCodes.EscalationBlocked, expectedAtLeast: 2)).Count);
    }

    [Fact]
    public async Task Staff_and_roles_of_another_business_are_not_found()
    {
        var w = await WorldAsync();
        var other = await RbacTestData.CreateBusinessWithOwnerAsync(api);

        var foreignStaff = await Grant(w.Owner.AccessToken, other.Owner, w.Setup.CashierRoleId);
        var foreignRole = await Grant(w.Owner.AccessToken, w.Staff, other.CashierRoleId);

        Assert.Equal("staff_not_found", await Code(foreignStaff));
        Assert.Equal("role_not_found", await Code(foreignRole));
    }
}
