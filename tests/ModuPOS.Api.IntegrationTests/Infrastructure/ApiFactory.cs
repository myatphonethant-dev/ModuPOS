using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Authorization;
using ModuPOS.Shared.Auditing;
using ModuPOS.Database;
using ModuPOS.LicenseManagement.Interfaces;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Runs the real API against a throwaway SQL Server database. Uses LocalDB by default;
/// set MODUPOS_TEST_SQL to a connection string (without Database=) to use another server.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"ModuPOS_Test_{Guid.NewGuid():N}";

    public string ConnectionString { get; }

    public ApiFactory()
    {
        var server = Environment.GetEnvironmentVariable("MODUPOS_TEST_SQL")
            ?? @"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True";
        ConnectionString = $"{server.TrimEnd(';')};Database={_databaseName}";
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Jwt:AllowEphemeralKey", "true");
        builder.UseSetting("Auth:SignInRateLimitPerMinute", "100000");
        builder.UseSetting("Auth:Audit:DedupWindowSeconds", "1");

        builder.ConfigureTestServices(services =>
        {
            // No license token in tests: every feature and license is valid unless a test says otherwise.
            services.AddSingleton(Features);
            services.AddScoped<IFeatureAvailability>(sp => sp.GetRequiredService<TestFeatures>());
            services.AddSingleton(Licenses);
            services.AddSingleton<ITenantLicenseStatus>(Licenses);

            services.AddControllers().AddApplicationPart(typeof(TestEndpointsController).Assembly);

            // Wrap the real audit writer so tests can make critical audit rows fail to save.
            var real = services.Single(d => d.ServiceType == typeof(IAuditWriter));
            services.Remove(real);
            services.AddSingleton(AuditFaults);
            services.AddScoped<IAuditWriter>(sp => new FaultyAuditWriter(
                (IAuditWriter)ActivatorUtilities.CreateInstance(sp, real.ImplementationType!),
                sp.GetRequiredService<DbContext>(),
                AuditFaults));
        });
    }

    public AuditFaults AuditFaults { get; } = new();

    public TestFeatures Features { get; } = new();

    public TestLicenses Licenses { get; } = new();

    /// <summary>
    /// Turns a feature off for one business and bumps its authorization version, as an admin
    /// change in feature management would, so cached permissions refresh.
    /// </summary>
    public async Task DisableFeatureAsync(Guid businessId, string featureCode)
    {
        Features.Disable(businessId, featureCode);
        await BumpVersionAsync(businessId);
    }

    public async Task EnableFeatureAsync(Guid businessId, string featureCode)
    {
        Features.Enable(businessId, featureCode);
        await BumpVersionAsync(businessId);
    }

    /// <summary>Simulates an admin change that affects permissions (as status changes would).</summary>
    public async Task BumpVersionAsync(Guid businessId)
    {
        await using var scope = Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<AuthorizationVersionService>().IncrementAsync(businessId);
    }

    public ValueTask InitializeAsync()
    {
        _ = Server; // start the host (runs migrations)
        return ValueTask.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        var options = new DbContextOptionsBuilder<ModuPOSDbContext>().UseSqlServer(ConnectionString).Options;
        await using var db = new ModuPOSDbContext(options, new TenantContext());
        await db.Database.EnsureDeletedAsync();
    }

    public async Task<T> WithDbAsync<T>(Func<ModuPOSDbContext, Task<T>> action)
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<ModuPOSDbContext>());
    }

    public Task WithDbAsync(Func<ModuPOSDbContext, Task> action) =>
        WithDbAsync(async db => { await action(db); return true; });

}

public sealed class AuditFaults
{
    /// <summary>Business whose critical audit writes fail (null = none).</summary>
    public Guid? FailCriticalForBusiness { get; set; }
}

/// <summary>Adds a row that violates the Details JSON check, so the whole save fails like an outage would.</summary>
internal sealed class FaultyAuditWriter(IAuditWriter inner, DbContext db, AuditFaults faults) : IAuditWriter
{
    public void WriteCritical(AuditEntry entry)
    {
        if (faults.FailCriticalForBusiness is { } bid && entry.BusinessId == bid)
        {
            db.Set<AuditEntryRecord>().Add(new AuditEntryRecord { Code = entry.Code, OccurredAt = entry.OccurredAt, BusinessId = bid, Details = "not json" });
            return;
        }

        inner.WriteCritical(entry);
    }

    public void Enqueue(AuditEntry entry) => inner.Enqueue(entry);
}

public sealed class TestLicenses : ITenantLicenseStatus
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Guid, bool> _invalid = new();

    public void Invalidate(Guid businessId) => _invalid[businessId] = true;

    public void Restore(Guid businessId) => _invalid.TryRemove(businessId, out _);

    public Task<bool> IsValidAsync(Guid businessId, CancellationToken ct = default) =>
        Task.FromResult(!_invalid.ContainsKey(businessId));
}

public sealed class TestFeatures : IFeatureAvailability
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<(Guid, string), bool> _disabled = new();

    public void Disable(Guid businessId, string featureCode) => _disabled[(businessId, featureCode)] = true;

    public void Enable(Guid businessId, string featureCode) => _disabled.TryRemove((businessId, featureCode), out _);

    public Task<bool> IsEnabledAsync(Guid businessId, string featureCode, CancellationToken ct = default) =>
        Task.FromResult(!_disabled.ContainsKey((businessId, featureCode)));
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
