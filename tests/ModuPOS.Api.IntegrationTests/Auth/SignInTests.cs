using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ModuPOS.Api.IntegrationTests.Infrastructure;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Sessions;

namespace ModuPOS.Api.IntegrationTests.Auth;

/// <summary>User Story 1: sign in, stay signed in, and sign out.</summary>
[Collection(ApiCollection.Name)]
public class SignInTests(ApiFactory api)
{
    private readonly HttpClient _client = api.CreateClient();

    private static async Task<ProblemBody> Problem(HttpResponseMessage r) =>
        (await r.Content.ReadFromJsonAsync<ProblemBody>())!;

    private Task<HttpResponseMessage> Me(string accessToken) =>
        _client.SendAsync(TestData.Authorized(HttpMethod.Get, "/api/auth/me", accessToken));

    private Task<HttpResponseMessage> Refresh(string refreshToken) =>
        _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

    // Scenario 1
    [Fact]
    public async Task Active_account_with_correct_credentials_signs_in()
    {
        var account = await TestData.CreateStaffAsync(api);

        var body = await TestData.SignInOkAsync(_client, account);

        Assert.False(string.IsNullOrEmpty(body.AccessToken));
        Assert.False(string.IsNullOrEmpty(body.RefreshToken));
        Assert.InRange(body.AccessTokenExpiresAt, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
        Assert.InRange(body.RefreshTokenExpiresAt, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7).AddMinutes(1));
        Assert.Equal("portal_selected", body.Next);
        Assert.Single(body.Profiles);

        var me = await Me(body.AccessToken);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var json = await me.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(account.UserId, json.GetProperty("user").GetProperty("id").GetGuid());
        Assert.Equal(account.StaffProfileId, json.GetProperty("activeProfile").GetProperty("profileId").GetGuid());

        var audit = await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.SignInSucceeded);
        var entry = Assert.Single(audit);
        Assert.Equal(account.BusinessId, entry.BusinessId);
        Assert.NotNull(entry.SessionId);
    }

    [Fact]
    public async Task Email_works_as_login()
    {
        var account = await TestData.CreateStaffAsync(api);

        var response = await TestData.SignInAsync(_client, account.Email.ToUpperInvariant(), account.Password);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Scenario 2
    [Fact]
    public async Task Wrong_password_and_unknown_user_get_the_same_generic_error()
    {
        var account = await TestData.CreateStaffAsync(api);

        var wrongPassword = await TestData.SignInAsync(_client, account.Username, "not-the-password");
        var unknownUser = await TestData.SignInAsync(_client, "nobody-" + Guid.NewGuid().ToString("N"), "whatever");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownUser.StatusCode);

        var p1 = await Problem(wrongPassword);
        var p2 = await Problem(unknownUser);
        Assert.Equal("invalid_credentials", p1.Code);
        Assert.Equal(p1.Code, p2.Code);
        Assert.Equal(p1.Title, p2.Title);

        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.SignInFailed));
        Assert.Contains("wrong_credentials", audit.Details);
        Assert.DoesNotContain("not-the-password", audit.Details);
    }

    // Scenario 3
    [Theory]
    [InlineData(AccountStatus.Suspended)]
    [InlineData(AccountStatus.Deactivated)]
    public async Task Inactive_account_is_rejected_even_with_correct_credentials(AccountStatus status)
    {
        var account = await TestData.CreateStaffAsync(api, status);

        var response = await TestData.SignInAsync(_client, account.Username, account.Password);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("account_inactive", (await Problem(response)).Code);
        Assert.Single(await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.SignInFailed));
    }

    [Fact]
    public async Task Inactive_account_with_wrong_password_gets_generic_error()
    {
        var account = await TestData.CreateStaffAsync(api, AccountStatus.Suspended);

        var response = await TestData.SignInAsync(_client, account.Username, "wrong");

        Assert.Equal("invalid_credentials", (await Problem(response)).Code);
    }

    // FR-004 / edge case: lockout
    [Fact]
    public async Task Fifth_wrong_password_locks_the_account()
    {
        var account = await TestData.CreateStaffAsync(api);

        for (var i = 0; i < 4; i++)
            Assert.Equal(HttpStatusCode.Unauthorized, (await TestData.SignInAsync(_client, account.Username, "wrong")).StatusCode);

        var fifth = await TestData.SignInAsync(_client, account.Username, "wrong");
        Assert.Equal((HttpStatusCode)423, fifth.StatusCode);
        var json = await fifth.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("account_locked", json.GetProperty("code").GetString());
        Assert.True(json.GetProperty("lockedUntil").GetDateTime() > DateTime.UtcNow.AddMinutes(14));

        // Even the right password is refused while locked.
        var correct = await TestData.SignInAsync(_client, account.Username, account.Password);
        Assert.Equal((HttpStatusCode)423, correct.StatusCode);

        Assert.Single(await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.AccountLocked));
    }

    // Scenario 4
    [Fact]
    public async Task Renewal_issues_a_new_pair_without_the_password()
    {
        var account = await TestData.CreateStaffAsync(api);
        var first = await TestData.SignInOkAsync(_client, account);

        var response = await Refresh(first.RefreshToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var second = (await response.Content.ReadFromJsonAsync<SignInBody>())!;
        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.Equal(HttpStatusCode.OK, (await Me(second.AccessToken)).StatusCode);

        // Rotation is not audited (FR-037).
        await Task.Delay(1500);
        var rows = await api.WithDbAsync(db => db.Set<AuditEntryRecord>().IgnoreQueryFilters()
            .Where(a => a.ActorUserId == account.UserId).Select(a => a.Code).ToListAsync());
        Assert.Equal([AuditCodes.SignInSucceeded], rows);
    }

    [Fact]
    public async Task Unknown_refresh_token_is_invalid()
    {
        var response = await Refresh("definitely-not-a-token");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("refresh_invalid", (await Problem(response)).Code);
    }

    // Scenario 5
    [Fact]
    public async Task Sign_out_revokes_the_session_immediately()
    {
        var account = await TestData.CreateStaffAsync(api);
        var tokens = await TestData.SignInOkAsync(_client, account);
        Assert.Equal(HttpStatusCode.OK, (await Me(tokens.AccessToken)).StatusCode); // warms the session cache

        var signOut = await _client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/auth/sign-out", tokens.AccessToken));

        Assert.Equal(HttpStatusCode.NoContent, signOut.StatusCode);
        var me = await Me(tokens.AccessToken);
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
        Assert.Equal("not_signed_in", (await Problem(me)).Code);
        Assert.Equal("refresh_invalid", (await Problem(await Refresh(tokens.RefreshToken))).Code);

        var session = await api.WithDbAsync(db => db.Set<Session>().SingleAsync(s => s.UserAccountId == account.UserId));
        Assert.Equal(SessionStatus.Revoked, session.Status);
        Assert.Equal(SessionRevokedReason.SignOut, session.RevokedReason);
        Assert.Null(session.ActiveBranchId);

        Assert.Single(await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.SignOut));
    }

    [Fact]
    public async Task Protected_endpoint_without_token_is_not_signed_in()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("not_signed_in", (await Problem(response)).Code);
    }

    // Edge case: many devices
    [Fact]
    public async Task Sign_out_on_one_device_keeps_others_and_sign_out_all_ends_every_session()
    {
        var account = await TestData.CreateStaffAsync(api);
        var deviceA = await TestData.SignInOkAsync(_client, account);
        var deviceB = await TestData.SignInOkAsync(_client, account);
        var deviceC = await TestData.SignInOkAsync(_client, account);

        await _client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/auth/sign-out", deviceA.AccessToken));
        Assert.Equal(HttpStatusCode.OK, (await Me(deviceB.AccessToken)).StatusCode);

        var all = await _client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/auth/sign-out-all", deviceB.AccessToken));

        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        var json = await all.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, json.GetProperty("sessionsEnded").GetInt32());
        Assert.Equal(HttpStatusCode.Unauthorized, (await Me(deviceB.AccessToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Me(deviceC.AccessToken)).StatusCode);

        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.SignOutAll));
        Assert.Contains("\"sessionsEnded\":2", audit.Details);
    }

    // Scenario 6
    [Fact]
    public async Task Reusing_a_renewal_credential_revokes_the_whole_session()
    {
        var account = await TestData.CreateStaffAsync(api);
        var original = await TestData.SignInOkAsync(_client, account);
        var rotated = (await (await Refresh(original.RefreshToken)).Content.ReadFromJsonAsync<SignInBody>())!;

        var reuse = await Refresh(original.RefreshToken);

        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
        Assert.Equal("refresh_reused", (await Problem(reuse)).Code);
        Assert.Equal("refresh_invalid", (await Problem(await Refresh(rotated.RefreshToken))).Code);
        Assert.Equal(HttpStatusCode.Unauthorized, (await Me(rotated.AccessToken)).StatusCode);

        var session = await api.WithDbAsync(db => db.Set<Session>().SingleAsync(s => s.UserAccountId == account.UserId));
        Assert.Equal(SessionRevokedReason.RenewalReuse, session.RevokedReason);

        var audit = Assert.Single(await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.RenewalReuseDetected));
        Assert.Equal(ModuPOS.Shared.Auditing.AuditSeverity.High, audit.Severity);
    }

    [Fact]
    public async Task Account_without_profiles_signs_in_with_no_access()
    {
        var account = await TestData.CreateStaffAsync(api, withStaffProfile: false);

        var body = await TestData.SignInOkAsync(_client, account);

        Assert.Equal("no_access", body.Next);
        Assert.Empty(body.Profiles);
    }

    [Fact]
    public async Task Audit_entries_never_contain_credentials()
    {
        var account = await TestData.CreateStaffAsync(api);
        var tokens = await TestData.SignInOkAsync(_client, account);
        await TestData.SignInAsync(_client, account.Username, "wrong-" + account.Password);
        await TestData.WaitForAuditAsync(api, account.UserId, AuditCodes.SignInFailed);

        var details = await api.WithDbAsync(db => db.Set<AuditEntryRecord>().IgnoreQueryFilters()
            .Where(a => a.ActorUserId == account.UserId).Select(a => a.Details).ToListAsync());

        Assert.All(details, d =>
        {
            Assert.DoesNotContain(account.Password, d);
            Assert.DoesNotContain(tokens.RefreshToken, d);
            Assert.DoesNotContain(tokens.AccessToken, d);
        });
    }
}
