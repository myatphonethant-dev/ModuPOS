using System.Threading.RateLimiting;
using ModuPOS.Api.Controllers.Auth;
using ModuPOS.Api.Infrastructure;
using ModuPOS.Auth;
using ModuPOS.Auth.Http;
using ModuPOS.Database;
using ModuPOS.FeatureManagement;
using ModuPOS.LicenseManagement;
using ModuPOS.Organization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddLicenseManagement(builder.Configuration);
builder.Services.AddFeatureManagement();
builder.Services.AddModuPOSDatabase(builder.Configuration);
builder.Services.AddOrganization();
builder.Services.AddModuPOSAuth(builder.Configuration);

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy(AuthController.SignInRateLimitPolicy, context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = ModuPOS.Auth.DependencyInjection.SignInRateLimit(context.RequestServices),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.OnRejected = (context, _) =>
        new ValueTask(ProblemResponses.WriteAsync(context.HttpContext, AuthErrors.TooManyRequests));
});

var app = builder.Build();

await DevSeeder.MigrateAndSeedAsync(app);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseRateLimiter();
app.UseModuPOSAccessPipeline();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

/// <summary>Entry point, visible to integration tests.</summary>
public partial class Program;
