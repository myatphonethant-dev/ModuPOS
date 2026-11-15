using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ModuPOS.Api.IntegrationTests.Infrastructure;
using ModuPOS.Auth.Accounts;
using ModuPOS.Organization.Entities;

namespace ModuPOS.Api.IntegrationTests.Auth;

/// <summary>User Story 2: portal routing for people with more than one profile.</summary>
[Collection(ApiCollection.Name)]
public class PortalTests(ApiFactory api)
{
    private readonly HttpClient _client = api.CreateClient();

    private async Task<Guid> AddProfileAsync(TestAccount account, ProfileType type, ProfileStatus status = ProfileStatus.Active, Guid? businessId = null) =>
        await api.WithDbAsync(async db =>
        {
            var profile = new Profile
            {
                UserAccountId = account.UserId,
                Type = type,
                BusinessId = type == ProfileType.PlatformAdmin ? null : businessId ?? account.BusinessId,
                Status = status
            };
            db.Add(profile);
            await db.SaveChangesAsync();
            return profile.Id;
        });

    private async Task<JsonElement> MeAsync(string accessToken)
    {
        var response = await _client.SendAsync(TestData.Authorized(HttpMethod.Get, "/api/auth/me", accessToken));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private Task<HttpResponseMessage> ChooseAsync(string accessToken, Guid profileId) =>
        _client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/auth/portal", accessToken, new { profileId }));

    // Scenario 1
    [Fact]
    public async Task Single_profile_goes_straight_to_its_portal()
    {
        var account = await TestData.CreateStaffAsync(api);

        var body = await TestData.SignInOkAsync(_client, account);

        Assert.Equal("portal_selected", body.Next);
        var me = await MeAsync(body.AccessToken);
        Assert.Equal("Staff", me.GetProperty("activeProfile").GetProperty("type").GetString());
    }

    // Scenario 2
    [Fact]
    public async Task Staff_and_member_profiles_are_offered_a_choice_and_each_choice_opens_only_that_portal()
    {
        var account = await TestData.CreateStaffAsync(api);
        var memberId = await AddProfileAsync(account, ProfileType.Member);

        var body = await TestData.SignInOkAsync(_client, account);

        Assert.Equal("choose_portal", body.Next);
        Assert.Equal(2, body.Profiles.Count);
        Assert.Equal(JsonValueKind.Null, (await MeAsync(body.AccessToken)).GetProperty("activeProfile").ValueKind);

        var asStaff = await ChooseAsync(body.AccessToken, account.StaffProfileId!.Value);
        Assert.Equal(HttpStatusCode.OK, asStaff.StatusCode);
        var staffToken = (await asStaff.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        var staffMe = (await MeAsync(staffToken)).GetProperty("activeProfile");
        Assert.Equal("Staff", staffMe.GetProperty("type").GetString());
        Assert.Equal(account.StaffProfileId, staffMe.GetProperty("profileId").GetGuid());

        var asMember = await ChooseAsync(staffToken, memberId);
        var memberToken = (await asMember.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;
        var memberMe = await MeAsync(memberToken);
        Assert.Equal("Member", memberMe.GetProperty("activeProfile").GetProperty("type").GetString());
        Assert.Empty(memberMe.GetProperty("permissions").EnumerateArray()); // no staff permissions in the member portal
    }

    [Fact]
    public async Task Renewal_keeps_the_chosen_portal()
    {
        var account = await TestData.CreateStaffAsync(api);
        await AddProfileAsync(account, ProfileType.Member);
        var body = await TestData.SignInOkAsync(_client, account);
        await ChooseAsync(body.AccessToken, account.StaffProfileId!.Value);

        var renewed = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = body.RefreshToken });
        var token = (await renewed.Content.ReadFromJsonAsync<SignInBody>())!.AccessToken;

        Assert.Equal(account.StaffProfileId, (await MeAsync(token)).GetProperty("activeProfile").GetProperty("profileId").GetGuid());
    }

    // Scenario 3
    [Fact]
    public async Task Inactive_profile_cannot_be_chosen()
    {
        var account = await TestData.CreateStaffAsync(api);
        var inactiveMember = await AddProfileAsync(account, ProfileType.Member, ProfileStatus.Inactive);
        var body = await TestData.SignInOkAsync(_client, account);

        Assert.Single(body.Profiles); // inactive profiles are not offered

        var response = await ChooseAsync(body.AccessToken, inactiveMember);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("profile_inactive", (await response.Content.ReadFromJsonAsync<ProblemBody>())!.Code);
    }

    [Fact]
    public async Task Profile_of_a_suspended_business_is_not_offered_or_selectable()
    {
        var account = await TestData.CreateStaffAsync(api);
        await api.WithDbAsync(async db =>
        {
            var business = await db.Set<Business>().FindAsync(account.BusinessId);
            business!.Status = BusinessStatus.Suspended;
            await db.SaveChangesAsync();
        });

        var body = await TestData.SignInOkAsync(_client, account);

        Assert.Equal("no_access", body.Next);
        var response = await ChooseAsync(body.AccessToken, account.StaffProfileId!.Value);
        Assert.Equal("profile_inactive", (await response.Content.ReadFromJsonAsync<ProblemBody>())!.Code);
    }

    [Fact]
    public async Task Another_persons_profile_is_not_found()
    {
        var me = await TestData.CreateStaffAsync(api);
        var someoneElse = await TestData.CreateStaffAsync(api);
        var body = await TestData.SignInOkAsync(_client, me);

        var response = await ChooseAsync(body.AccessToken, someoneElse.StaffProfileId!.Value);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("profile_not_found", (await response.Content.ReadFromJsonAsync<ProblemBody>())!.Code);
    }

    // Scenario 4
    [Fact]
    public async Task No_active_profile_means_no_portal()
    {
        var account = await TestData.CreateStaffAsync(api, withStaffProfile: false);
        await AddProfileAsync(account, ProfileType.Member, ProfileStatus.Inactive);

        var body = await TestData.SignInOkAsync(_client, account);

        Assert.Equal("no_access", body.Next);
        Assert.Empty(body.Profiles);
        Assert.Equal(JsonValueKind.Null, (await MeAsync(body.AccessToken)).GetProperty("activeProfile").ValueKind);
    }
}
