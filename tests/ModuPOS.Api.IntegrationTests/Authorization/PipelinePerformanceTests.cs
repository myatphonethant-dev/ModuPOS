using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ModuPOS.Api.IntegrationTests.Infrastructure;

namespace ModuPOS.Api.IntegrationTests.Authorization;

/// <summary>
/// Smoke check for the plan's target: the full pipeline adds at most ~10 ms at p95 once its
/// caches are warm. Measured end to end through the in-memory test server on an endpoint
/// that does no work of its own, so the number is an upper bound on pipeline cost.
/// </summary>
[Collection(ApiCollection.Name)]
public class PipelinePerformanceTests(ApiFactory api)
{
    [Fact]
    public async Task Warm_pipeline_p95_is_within_10_ms()
    {
        var client = api.CreateClient();
        var setup = await RbacTestData.CreateBusinessWithOwnerAsync(api);
        var staff = await RbacTestData.AddStaffAsync(api, setup.BusinessId);
        var branch = await RbacTestData.AddBranchAsync(api, setup.BusinessId);
        await RbacTestData.AssignBranchAsync(api, staff, branch);
        await RbacTestData.GrantInDbAsync(api, staff, setup.ManagerRoleId);
        var tokens = await TestData.SignInOkAsync(client, staff);
        var select = await client.SendAsync(TestData.Authorized(HttpMethod.Post, "/api/auth/branch", tokens.AccessToken, new { branchId = branch }));
        var token = (await select.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString()!;

        for (var i = 0; i < 50; i++)
            Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(TestData.Authorized(HttpMethod.Get, "/test/branch-action", token))).StatusCode);

        var samples = new List<double>();
        for (var i = 0; i < 300; i++)
        {
            var sw = Stopwatch.StartNew();
            var response = await client.SendAsync(TestData.Authorized(HttpMethod.Get, "/test/branch-action", token));
            sw.Stop();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            samples.Add(sw.Elapsed.TotalMilliseconds);
        }

        samples.Sort();
        var p95 = samples[(int)(samples.Count * 0.95)];
        TestContext.Current.SendDiagnosticMessage($"Pipeline p95 = {p95:F2} ms, median = {samples[samples.Count / 2]:F2} ms");
        Assert.True(p95 <= 10, $"p95 was {p95:F2} ms");
    }
}
