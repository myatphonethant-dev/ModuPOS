using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ModuPOS.Api.IntegrationTests.Infrastructure;
using ModuPOS.Auth.Auditing;

namespace ModuPOS.Api.IntegrationTests.Rbac;

/// <summary>User Story 3: business admins create and edit custom roles.</summary>
[Collection(ApiCollection.Name)]
public class RoleTests(ApiFactory api)
{
    private readonly HttpClient _client = api.CreateClient();

    private Task<HttpResponseMessage> Send(HttpMethod method, string url, string token, object? body = null) =>
        _client.SendAsync(TestData.Authorized(method, url, token, body));

    private static async Task<string?> Code(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<ProblemBody>())!.Code;

    private async Task<(OwnerSetup Setup, SignInBody Tokens)> OwnerAsync()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        return (setup, await TestData.SignInOkAsync(_client, setup.Owner));
    }

    // Scenario 1
    [Fact]
    public async Task Owner_creates_a_role_and_it_is_listed_for_assignment()
    {
        var (setup, owner) = await OwnerAsync();

        var created = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Senior Cashier", "role.view", "audit.view");

        var roleId = created.GetProperty("id").GetGuid();
        Assert.Equal(["audit.view", "role.view"], created.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).Order());

        var list = await (await Send(HttpMethod.Get, "/api/roles", owner.AccessToken)).Content.ReadFromJsonAsync<JsonElement>();
        var names = list.EnumerateArray().Select(r => r.GetProperty("name").GetString()).ToList();
        Assert.Contains("Senior Cashier", names);
        Assert.Contains("Owner", names);
        Assert.Contains("Manager", names);
        Assert.Contains("Cashier", names);

        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.RoleCreated, targetId: roleId.ToString()));
        Assert.Equal(setup.BusinessId, audit.BusinessId);
        Assert.Contains("Senior Cashier", audit.Details);
    }

    // Scenario 2
    [Fact]
    public async Task Duplicate_name_in_the_same_business_is_rejected_ignoring_case()
    {
        var (_, owner) = await OwnerAsync();
        await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Stock Lead", "role.view");

        var duplicate = await Send(HttpMethod.Post, "/api/roles", owner.AccessToken, new { name = "stock lead", permissions = new[] { "role.view" } });

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
        Assert.Equal("role_name_taken", await Code(duplicate));
    }

    [Fact]
    public async Task Same_name_in_another_business_is_allowed()
    {
        var (_, ownerA) = await OwnerAsync();
        var (_, ownerB) = await OwnerAsync();

        await RbacTestData.CreateRoleAsync(_client, ownerA.AccessToken, "Floor Lead", "role.view");
        await RbacTestData.CreateRoleAsync(_client, ownerB.AccessToken, "Floor Lead", "role.view");
    }

    // Scenario 3
    [Fact]
    public async Task Missing_dependency_is_added_and_reported()
    {
        var (_, owner) = await OwnerAsync();

        var created = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Role Admin", "role.manage");

        Assert.Equal(["role.view"], created.GetProperty("addedDependencies").EnumerateArray().Select(p => p.GetString()));
        Assert.Equal(["role.manage", "role.view"], created.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).Order());
    }

    [Fact]
    public async Task Unknown_permission_is_rejected()
    {
        var (_, owner) = await OwnerAsync();

        var response = await Send(HttpMethod.Post, "/api/roles", owner.AccessToken, new { name = "Bad", permissions = new[] { "made.up" } });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("permission_unknown", await Code(response));
    }

    // Scenario 4
    [Fact]
    public async Task Removing_a_permission_takes_effect_for_holders_on_their_next_action()
    {
        var (setup, owner) = await OwnerAsync();
        var created = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Viewer", "role.view");
        var roleId = created.GetProperty("id").GetGuid();

        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, staff, roleId);
        var staffTokens = await TestData.SignInOkAsync(_client, staff);
        Assert.Equal(HttpStatusCode.OK, (await Send(HttpMethod.Get, "/api/roles", staffTokens.AccessToken)).StatusCode);

        var update = await Send(HttpMethod.Put, $"/api/roles/{roleId}", owner.AccessToken, new
        {
            name = "Viewer",
            permissions = new[] { "audit.view" },
            version = created.GetProperty("version").GetString()
        });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);

        // Same access token, no sign-out: refused within 5 seconds (SC-003).
        Assert.True(await RbacTestData.EventuallyAsync(async () =>
            (await Send(HttpMethod.Get, "/api/roles", staffTokens.AccessToken)).StatusCode == HttpStatusCode.Forbidden));
        Assert.Equal(["audit.view"], await RbacTestData.PermissionsAsync(_client, staffTokens.AccessToken));

        var changed = Assert.Single(await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.RolePermissionsChanged, targetId: roleId.ToString()));
        Assert.Contains("\"removed\":[\"role.view\"]", changed.Details);
        Assert.Contains("\"added\":[\"audit.view\"]", changed.Details);
        Assert.Contains("\"staffAffected\":1", changed.Details);
    }

    [Fact]
    public async Task Rename_is_audited_with_before_and_after()
    {
        var (setup, owner) = await OwnerAsync();
        var created = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Old Name", "role.view");
        var roleId = created.GetProperty("id").GetGuid();

        var update = await Send(HttpMethod.Put, $"/api/roles/{roleId}", owner.AccessToken, new
        {
            name = "New Name",
            description = "Renamed",
            permissions = new[] { "role.view" },
            version = created.GetProperty("version").GetString()
        });

        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.RoleUpdated, targetId: roleId.ToString()));
        Assert.Contains("Old Name", audit.Details);
        Assert.Contains("New Name", audit.Details);
        Assert.Empty(await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.RolePermissionsChanged, expectedAtLeast: 0, targetId: roleId.ToString()));
    }

    // FR-022
    [Fact]
    public async Task Second_edit_with_a_stale_version_is_rejected()
    {
        var (_, owner) = await OwnerAsync();
        var created = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Shared Role", "role.view");
        var roleId = created.GetProperty("id").GetGuid();
        var version = created.GetProperty("version").GetString();

        var first = await Send(HttpMethod.Put, $"/api/roles/{roleId}", owner.AccessToken, new { name = "Shared Role", permissions = new[] { "role.view", "audit.view" }, version });
        var second = await Send(HttpMethod.Put, $"/api/roles/{roleId}", owner.AccessToken, new { name = "Shared Role", permissions = new[] { "role.view" }, version });

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("role_version_conflict", await Code(second));
    }

    // Scenario 5
    [Fact]
    public async Task Roles_of_another_business_are_invisible()
    {
        var (_, ownerA) = await OwnerAsync();
        var (_, ownerB) = await OwnerAsync();
        var secret = await RbacTestData.CreateRoleAsync(_client, ownerA.AccessToken, "Secret Role", "role.view");
        var secretId = secret.GetProperty("id").GetGuid();

        var list = await (await Send(HttpMethod.Get, "/api/roles", ownerB.AccessToken)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(list.EnumerateArray(), r => r.GetProperty("id").GetGuid() == secretId);

        var get = await Send(HttpMethod.Get, $"/api/roles/{secretId}", ownerB.AccessToken);
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);

        var put = await Send(HttpMethod.Put, $"/api/roles/{secretId}", ownerB.AccessToken, new { name = "Hijacked", permissions = new[] { "role.view" }, version = secret.GetProperty("version").GetString() });
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
    }

    // Scenario 6 (FR-018): permissions of disabled features never grant access.
    [Fact]
    public async Task Permissions_of_a_disabled_feature_are_unavailable_and_grant_nothing()
    {
        var (setup, owner) = await OwnerAsync();
        var manager = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, manager, setup.ManagerRoleId);
        var managerTokens = await TestData.SignInOkAsync(_client, manager);
        Assert.Contains("role.view", await RbacTestData.PermissionsAsync(_client, managerTokens.AccessToken));

        await api.DisableFeatureAsync(setup.BusinessId, "POS.AUTH");
        try
        {
            Assert.True(await RbacTestData.EventuallyAsync(async () =>
                (await RbacTestData.PermissionsAsync(_client, managerTokens.AccessToken)).Length == 0));
            Assert.Equal(HttpStatusCode.Forbidden, (await Send(HttpMethod.Get, "/api/roles", managerTokens.AccessToken)).StatusCode);
        }
        finally
        {
            await api.EnableFeatureAsync(setup.BusinessId, "POS.AUTH");
        }
    }

    [Fact]
    public async Task Permission_list_hides_platform_permissions_and_shows_availability()
    {
        var (_, owner) = await OwnerAsync();

        var list = await (await Send(HttpMethod.Get, "/api/permissions", owner.AccessToken)).Content.ReadFromJsonAsync<JsonElement>();
        var codes = list.EnumerateArray().Select(p => p.GetProperty("code").GetString()).ToList();

        Assert.Contains("role.manage", codes);
        Assert.DoesNotContain("platform.audit.view", codes);
        var manage = list.EnumerateArray().Single(p => p.GetProperty("code").GetString() == "role.manage");
        Assert.True(manage.GetProperty("available").GetBoolean());
        Assert.Equal(["role.view"], manage.GetProperty("dependsOn").EnumerateArray().Select(d => d.GetString()));
    }

    // FR-017
    [Fact]
    public async Task Role_manager_cannot_create_a_role_with_permissions_they_lack()
    {
        var (setup, owner) = await OwnerAsync();
        var maker = await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Role Maker", "role.manage");
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, staff, maker.GetProperty("id").GetGuid());
        var staffTokens = await TestData.SignInOkAsync(_client, staff);

        var response = await Send(HttpMethod.Post, "/api/roles", staffTokens.AccessToken, new { name = "Auditor", permissions = new[] { "audit.view" } });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("escalation_blocked", await Code(response));
        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, staff.UserId, AuditCodes.EscalationBlocked));
        Assert.Contains("audit.view", audit.Details);

        // Within their own permissions it works.
        await RbacTestData.CreateRoleAsync(_client, staffTokens.AccessToken, "Viewer Only", "role.view");
    }

    [Fact]
    public async Task Staff_without_role_permissions_cannot_manage_roles()
    {
        var (setup, _) = await OwnerAsync();
        var cashier = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, cashier, setup.CashierRoleId);
        var tokens = await TestData.SignInOkAsync(_client, cashier);

        var list = await Send(HttpMethod.Get, "/api/roles", tokens.AccessToken);
        var create = await Send(HttpMethod.Post, "/api/roles", tokens.AccessToken, new { name = "Mine", permissions = new[] { "role.view" } });

        Assert.Equal("permission_denied", await Code(list));
        Assert.Equal("permission_denied", await Code(create));
    }

    [Fact]
    public async Task Owner_role_cannot_be_renamed_or_changed()
    {
        var (setup, owner) = await OwnerAsync();
        var ownerRole = await (await Send(HttpMethod.Get, $"/api/roles/{setup.OwnerRoleId}", owner.AccessToken)).Content.ReadFromJsonAsync<JsonElement>();

        var response = await Send(HttpMethod.Put, $"/api/roles/{setup.OwnerRoleId}", owner.AccessToken, new
        {
            name = "Boss",
            permissions = new[] { "role.view" },
            version = ownerRole.GetProperty("version").GetString()
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("owner_protected", await Code(response));
        Assert.Single(await TestData.WaitForAuditAsync(api, setup.Owner.UserId, AuditCodes.OwnerProtectionBlocked, targetId: setup.OwnerRoleId.ToString()));
    }

    [Fact]
    public async Task Owner_holds_every_business_permission()
    {
        var (_, owner) = await OwnerAsync();

        var permissions = await RbacTestData.PermissionsAsync(_client, owner.AccessToken);

        Assert.Equal(["audit.view", "role.assign", "role.manage", "role.view"], permissions);
    }
}
