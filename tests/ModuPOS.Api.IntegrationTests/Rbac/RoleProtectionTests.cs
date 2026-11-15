using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Api.IntegrationTests.Infrastructure;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Roles;

namespace ModuPOS.Api.IntegrationTests.Rbac;

/// <summary>User Story 7: protect roles that are in use and system roles.</summary>
[Collection(ApiCollection.Name)]
public class RoleProtectionTests(ApiFactory api)
{
    private readonly HttpClient _client = api.CreateClient();

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string token, object? body = null) =>
        _client.SendAsync(TestData.Authorized(method, url, token, body));

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    private static async Task<string?> Code(HttpResponseMessage r) => (await Json(r)).GetProperty("code").GetString();

    private async Task<(OwnerSetup Setup, SignInBody Owner)> OwnerAsync()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        return (setup, await TestData.SignInOkAsync(_client, setup.Owner));
    }

    // Scenario 1
    [Fact]
    public async Task Role_held_by_staff_cannot_be_deleted_and_holders_are_listed()
    {
        var (setup, owner) = await OwnerAsync();
        var role = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Shift Lead", "role.view");
        var roleId = role.GetProperty("id").GetGuid();
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var branch = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        await RbacTestData.AssignBranchAsync(api, staff, branch);
        await RbacTestData.GrantInDbAsync(api, staff, roleId, branch);

        var response = await Send(HttpMethod.Delete, $"/api/roles/{roleId}", owner.AccessToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var body = await Json(response);
        Assert.Equal("role_in_use", body.GetProperty("code").GetString());
        var holder = Assert.Single(body.GetProperty("holders").EnumerateArray());
        Assert.Equal(staff.StaffProfileId, holder.GetProperty("staffProfileId").GetGuid());
        Assert.Equal(staff.Username, holder.GetProperty("name").GetString());
        Assert.Equal(branch, holder.GetProperty("branchId").GetGuid());
    }

    // Scenario 2
    [Fact]
    public async Task Unassigned_role_is_deleted_and_audited()
    {
        var (setup, owner) = await OwnerAsync();
        var role = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Temporary", "role.view", "audit.view");
        var roleId = role.GetProperty("id").GetGuid();

        var response = await Send(HttpMethod.Delete, $"/api/roles/{roleId}", owner.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Send(HttpMethod.Get, $"/api/roles/{roleId}", owner.AccessToken)).StatusCode);
        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.RoleDeleted, targetId: roleId.ToString()));
        Assert.Contains("Temporary", audit.Details);
        Assert.Contains("audit.view", audit.Details);
    }

    [Fact]
    public async Task Deleting_after_reassignment_succeeds()
    {
        var (setup, owner) = await OwnerAsync();
        var role = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Old Role", "role.view");
        var roleId = role.GetProperty("id").GetGuid();
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var grant = await Send(HttpMethod.Post, $"/api/staff/{staff.StaffProfileId}/role-assignments", owner.AccessToken, new { roleId });
        var assignmentId = (await Json(grant)).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.Conflict, (await Send(HttpMethod.Delete, $"/api/roles/{roleId}", owner.AccessToken)).StatusCode);

        await Send(HttpMethod.Delete, $"/api/staff/{staff.StaffProfileId}/role-assignments/{assignmentId}", owner.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, (await Send(HttpMethod.Delete, $"/api/roles/{roleId}", owner.AccessToken)).StatusCode);
    }

    // Scenario 3
    [Fact]
    public async Task Deactivated_role_grants_nothing_but_keeps_assignments_until_reactivated()
    {
        var (setup, owner) = await OwnerAsync();
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, staff, setup.ManagerRoleId);
        var staffTokens = await TestData.SignInOkAsync(_client, staff);
        Assert.Contains("role.assign", await RbacTestData.PermissionsAsync(_client, staffTokens.AccessToken));

        var off = await Send(HttpMethod.Post, $"/api/roles/{setup.ManagerRoleId}/deactivate", owner.AccessToken);

        Assert.Equal(HttpStatusCode.OK, off.StatusCode);
        Assert.Equal(1, (await Json(off)).GetProperty("affectedStaff").GetInt32());
        Assert.Empty(await RbacTestData.PermissionsAsync(_client, staffTokens.AccessToken));
        Assert.Equal(1, await api.WithDbAsync(db => db.Set<RoleAssignment>().IgnoreQueryFilters().CountAsync(a => a.RoleId == setup.ManagerRoleId)));

        var on = await Send(HttpMethod.Post, $"/api/roles/{setup.ManagerRoleId}/reactivate", owner.AccessToken);

        Assert.Equal(HttpStatusCode.OK, on.StatusCode);
        Assert.Contains("role.assign", await RbacTestData.PermissionsAsync(_client, staffTokens.AccessToken));
        Assert.Single(await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.RoleDeactivated, targetId: setup.ManagerRoleId.ToString()));
        Assert.Single(await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.RoleReactivated, targetId: setup.ManagerRoleId.ToString()));
    }

    // Scenario 4
    [Fact]
    public async Task Owner_role_cannot_be_deleted_or_deactivated()
    {
        var (setup, owner) = await OwnerAsync();

        var delete = await Send(HttpMethod.Delete, $"/api/roles/{setup.OwnerRoleId}", owner.AccessToken);
        var deactivate = await Send(HttpMethod.Post, $"/api/roles/{setup.OwnerRoleId}/deactivate", owner.AccessToken);

        Assert.Equal("owner_protected", await Code(delete));
        Assert.Equal("owner_protected", await Code(deactivate));
        Assert.Equal(2, (await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.OwnerProtectionBlocked, expectedAtLeast: 2, targetId: setup.OwnerRoleId.ToString())).Count);
        Assert.Contains("role.manage", await RbacTestData.PermissionsAsync(_client, owner.AccessToken));
    }

    // Scenario 5 + SC-007: no sequence of removals leaves a business without an active Owner.
    [Fact]
    public async Task Removing_owners_in_any_order_always_leaves_one()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        for (var i = 0; i < 3; i++)
        {
            var coOwner = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
            await RbacTestData.GrantInDbAsync(api, coOwner, setup.OwnerRoleId);
        }

        // A manager (role.assign) does the removals, so who acts never changes.
        var manager = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, manager, setup.ManagerRoleId);
        var managerToken = (await TestData.SignInOkAsync(_client, manager)).AccessToken;

        var assignments = await api.WithDbAsync(db => db.Set<RoleAssignment>().IgnoreQueryFilters()
            .Where(a => a.RoleId == setup.OwnerRoleId)
            .Select(a => new { a.Id, a.StaffProfileId })
            .ToListAsync());
        Assert.Equal(4, assignments.Count);

        var results = new List<HttpStatusCode>();
        foreach (var a in assignments.OrderBy(_ => Random.Shared.Next()))
            results.Add((await Send(HttpMethod.Delete, $"/api/staff/{a.StaffProfileId}/role-assignments/{a.Id}", managerToken)).StatusCode);

        Assert.Equal([HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.NoContent, HttpStatusCode.Conflict], results);
        Assert.Equal(1, await api.WithDbAsync(db => db.Set<RoleAssignment>().IgnoreQueryFilters().CountAsync(a => a.RoleId == setup.OwnerRoleId)));
    }

    [Fact]
    public async Task Inactive_staff_does_not_count_as_the_remaining_owner()
    {
        var (setup, owner) = await OwnerAsync();
        var inactiveCoOwner = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, inactiveCoOwner, setup.OwnerRoleId);
        await api.WithDbAsync(async db =>
        {
            (await db.Set<ModuPOS.Auth.Accounts.Profile>().FindAsync(inactiveCoOwner.StaffProfileId))!.Status = ModuPOS.Auth.Accounts.ProfileStatus.Inactive;
            await db.SaveChangesAsync();
        });
        var mine = await api.WithDbAsync(db => db.Set<RoleAssignment>().IgnoreQueryFilters()
            .SingleAsync(a => a.RoleId == setup.OwnerRoleId && a.StaffProfileId == setup.Owner.StaffProfileId));

        var response = await Send(HttpMethod.Delete, $"/api/staff/{setup.Owner.StaffProfileId}/role-assignments/{mine.Id}", owner.AccessToken);

        Assert.Equal("last_owner", await Code(response));
    }
}
