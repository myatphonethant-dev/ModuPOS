using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Api.IntegrationTests.Infrastructure;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Roles;

namespace ModuPOS.Api.IntegrationTests.Audit;

/// <summary>FR-033–FR-040: what is audited, who may see it, and how it is protected.</summary>
[Collection(ApiCollection.Name)]
public class AuditTests(ApiFactory api)
{
    private readonly HttpClient _client = api.CreateClient();

    private Task<HttpResponseMessage> Get(string url, string token) =>
        _client.SendAsync(TestData.Authorized(HttpMethod.Get, url, token));

    private static async Task<JsonElement> Json(HttpResponseMessage r) => await r.Content.ReadFromJsonAsync<JsonElement>();

    private async Task<(OwnerSetup Setup, SignInBody Owner)> OwnerAsync()
    {
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        return (setup, await TestData.SignInOkAsync(_client, setup.Owner));
    }

    private async Task<SignInBody> PlatformAdminAsync()
    {
        var admin = await TestData.CreateStaffAsync(api, withStaffProfile: false);
        await api.WithDbAsync(async db =>
        {
            db.Add(new Profile { UserAccountId = admin.UserId, Type = ProfileType.PlatformAdmin });
            await db.SaveChangesAsync();
        });
        return await TestData.SignInOkAsync(_client, admin);
    }

    // FR-039
    [Fact]
    public async Task Owner_sees_their_business_audit_only_and_viewing_is_not_audited()
    {
        var (setup, owner) = await OwnerAsync();
        var (other, otherOwner) = await OwnerAsync();
        await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Mine", "role.view");
        await RbacTestData.CreateRoleAsync(_client, otherOwner.AccessToken, "Theirs", "role.view");

        var response = await Get("/api/audit?code=ROLE.CREATED", owner.AccessToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await Json(response);
        var item = Assert.Single(page.GetProperty("items").EnumerateArray());
        Assert.Equal(setup.BusinessId, item.GetProperty("businessId").GetGuid());
        Assert.Equal("Mine", item.GetProperty("details").GetProperty("name").GetString());
        Assert.Equal(1, page.GetProperty("total").GetInt32());

        var codes = await api.WithDbAsync(db => db.Set<AuditEntryRecord>().IgnoreQueryFilters()
            .Where(a => a.BusinessId == setup.BusinessId).Select(a => a.Code).ToListAsync());
        Assert.DoesNotContain(codes, c => c.StartsWith("AUDIT."));
        Assert.NotEqual(setup.BusinessId, other.BusinessId);
    }

    [Fact]
    public async Task Audit_view_needs_the_audit_permission_and_sane_paging()
    {
        var (setup, owner) = await OwnerAsync();
        var cashier = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, cashier, setup.CashierRoleId);
        var cashierTokens = await TestData.SignInOkAsync(_client, cashier);

        Assert.Equal("permission_denied", (await Json(await Get("/api/audit", cashierTokens.AccessToken))).GetProperty("code").GetString());
        Assert.Equal(HttpStatusCode.BadRequest, (await Get("/api/audit?pageSize=500", owner.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Get($"/api/audit?from={DateTime.UtcNow.AddYears(-2):O}", owner.AccessToken)).StatusCode);
    }

    [Fact]
    public async Task Platform_view_needs_a_reason_and_is_itself_audited()
    {
        var (setup, owner) = await OwnerAsync();
        await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Visible", "role.view");
        var admin = await PlatformAdminAsync();
        var url = $"/api/platform/businesses/{setup.BusinessId}/audit";

        var noReason = await Get(url, admin.AccessToken);
        Assert.Equal(HttpStatusCode.BadRequest, noReason.StatusCode);
        Assert.Equal("reason_required", (await Json(noReason)).GetProperty("code").GetString());

        var ok = await Get($"{url}?reason=Support%20ticket%20123&code=ROLE.CREATED", admin.AccessToken);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        Assert.Single((await Json(ok)).GetProperty("items").EnumerateArray());

        Task<List<AuditEntryRecord>> Views() => api.WithDbAsync(db => db.Set<AuditEntryRecord>().IgnoreQueryFilters()
            .Where(a => a.Code == AuditCodes.AuditViewedByPlatform && a.BusinessId == setup.BusinessId).ToListAsync());
        Assert.True(await RbacTestData.EventuallyAsync(async () => (await Views()).Count == 1, timeoutMs: 10000));
        Assert.Contains("Support ticket 123", Assert.Single(await Views()).Details);

        // Business staff cannot use the platform view.
        Assert.Equal("permission_denied", (await Json(await Get($"{url}?reason=curious", owner.AccessToken))).GetProperty("code").GetString());
    }

    // FR-040
    [Fact]
    public async Task Role_change_is_refused_when_its_audit_entry_cannot_be_saved_but_sign_in_continues()
    {
        var (setup, owner) = await OwnerAsync();
        api.AuditFaults.FailCriticalForBusiness = setup.BusinessId;
        try
        {
            var create = await _client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/roles", owner.AccessToken,
                new { name = "Unrecorded", permissions = new[] { "role.view" } }));

            Assert.Equal(HttpStatusCode.ServiceUnavailable, create.StatusCode);
            Assert.Equal("audit_unavailable", (await Json(create)).GetProperty("code").GetString());
            Assert.False(await api.WithDbAsync(db => db.Set<Role>().IgnoreQueryFilters().AnyAsync(r => r.Name == "Unrecorded" && r.BusinessId == setup.BusinessId)));

            var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
            Assert.Equal(HttpStatusCode.OK, (await TestData.SignInAsync(_client, staff.Username, staff.Password)).StatusCode);
        }
        finally
        {
            api.AuditFaults.FailCriticalForBusiness = null;
        }

        await RbacTestData.CreateRoleAsync(_client, owner.AccessToken, "Recorded", "role.view");
    }

    // SC-005 / FR-037
    [Fact]
    public async Task Excluded_activity_creates_no_audit_entries_and_only_catalog_codes_exist()
    {
        var (setup, _) = await OwnerAsync();
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        await RbacTestData.GrantInDbAsync(api, staff, setup.ManagerRoleId);
        var tokens = await TestData.SignInOkAsync(_client, staff);

        await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = tokens.RefreshToken });       // renewal
        await _client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/auth/portal", tokens.AccessToken, new { profileId = staff.StaffProfileId })); // portal selection
        await Get("/api/auth/me", tokens.AccessToken);                                                         // read-only view
        await Get("/api/roles", tokens.AccessToken);                                                           // successful action
        await _client.GetAsync("/api/roles");                                                                  // not signed in
        api.Licenses.Invalidate(setup.BusinessId);
        await Get("/api/roles", tokens.AccessToken);                                                           // license refusal
        api.Licenses.Restore(setup.BusinessId);
        await api.DisableFeatureAsync(setup.BusinessId, "POS.AUTH");
        await Get("/api/roles", tokens.AccessToken);                                                           // feature refusal
        await api.EnableFeatureAsync(setup.BusinessId, "POS.AUTH");

        await Task.Delay(2500); // let the queue and 1 s dedup window flush
        var mine = await api.WithDbAsync(db => db.Set<AuditEntryRecord>().IgnoreQueryFilters()
            .Where(a => a.ActorUserId == staff.UserId).Select(a => a.Code).ToListAsync());
        Assert.Equal([AuditCodes.SignInSucceeded], mine);

        var allCodes = await api.WithDbAsync(db => db.Set<AuditEntryRecord>().IgnoreQueryFilters().Select(a => a.Code).Distinct().ToListAsync());
        Assert.All(allCodes, c => Assert.Contains(c, AuditCodes.All));
    }

    // FR-036
    [Fact]
    public async Task Audit_rows_cannot_be_updated_or_deleted_outside_retention()
    {
        var account = await TestData.CreateStaffAsync(api);
        await TestData.SignInOkAsync(_client, account);
        var row = Assert.Single(await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.SignInSucceeded));

        var update = await Assert.ThrowsAsync<SqlException>(() => api.WithDbAsync(db =>
            db.Database.ExecuteSqlInterpolatedAsync($"UPDATE [AuditEntries] SET [RepeatCount] = 99 WHERE [Id] = {row.Id}")));
        Assert.Contains("append-only", update.Message);

        var delete = await Assert.ThrowsAsync<SqlException>(() => api.WithDbAsync(db =>
            db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM [AuditEntries] WHERE [Id] = {row.Id}")));
        Assert.Contains("retention", delete.Message);

        Assert.True(await api.WithDbAsync(db => db.Set<AuditEntryRecord>().IgnoreQueryFilters().AnyAsync(a => a.Id == row.Id && a.RepeatCount == 1)));
    }

    [Fact]
    public async Task Retention_job_removes_only_entries_older_than_a_year()
    {
        var oldId = Guid.NewGuid();
        var recentId = Guid.NewGuid();
        await api.WithDbAsync(async db =>
        {
            foreach (var (id, when) in new[] { (oldId, DateTime.UtcNow.AddDays(-400)), (recentId, DateTime.UtcNow.AddDays(-300)) })
            {
                await db.Database.ExecuteSqlInterpolatedAsync($$"""
                    INSERT INTO [AuditEntries] ([Id],[Code],[Severity],[OccurredAt],[Outcome],[Details],[RepeatCount])
                    VALUES ({{id}}, 'AUTH.SIGNOUT', 'Info', {{when}}, 'Succeeded', '{}', 1)
                    """);
            }
        });

        await using var scope = api.Services.CreateAsyncScope();
        var removed = await scope.ServiceProvider.GetRequiredService<AuditRetentionJob>().RunOnceAsync(CancellationToken.None);

        Assert.True(removed >= 1);
        var remaining = await api.WithDbAsync(db => db.Set<AuditEntryRecord>().IgnoreQueryFilters()
            .Where(a => a.Id == oldId || a.Id == recentId).Select(a => a.Id).ToListAsync());
        Assert.Equal([recentId], remaining);
    }
}
