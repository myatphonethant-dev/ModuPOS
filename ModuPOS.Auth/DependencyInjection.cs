using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ModuPOS.Auth.Accounts;
using ModuPOS.Auth.Auditing;
using ModuPOS.Auth.Authorization;
using ModuPOS.Auth.Permissions;
using ModuPOS.Auth.Portals;
using ModuPOS.Auth.Roles;
using ModuPOS.Auth.Sessions;
using ModuPOS.Auth.Tokens;
using ModuPOS.Shared.Auditing;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Auth;

public static class DependencyInjection
{
    /// <summary>
    /// Registers authentication, sessions, permissions, the access pipeline and auditing.
    /// Requires a scoped EF <c>DbContext</c> (see ModuPOS.Database) and IFeatureManager.
    /// </summary>
    public static IServiceCollection AddModuPOSAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<AuthOptions>(configuration.GetSection(AuthOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);
        services.AddHybridCache();

        // Request context
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

        // Tokens
        services.AddSingleton<JwtKeyProvider>();
        services.AddSingleton<JwtTokenService>();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<JwtTokenService>((o, jwt) =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = jwt.ValidationParameters();
            });

        // Accounts and sessions
        services.AddSingleton<PasswordService>();
        services.AddScoped<SessionStatusCache>();
        services.AddScoped<SessionTokens>();
        services.AddScoped<SignInService>();
        services.AddScoped<RefreshService>();
        services.AddScoped<SignOutService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<PortalService>();
        services.AddScoped<BranchContextService>();
        services.AddScoped<SessionRevocationService>();
        services.AddScoped<CrossTenantGuard>();
        services.AddScoped<AccessStatusService>();
        services.AddScoped<PipelineServices>();

        // Roles and assignments
        services.AddScoped<DefaultRolesProvisioner>();
        services.AddScoped<RoleService>();
        services.AddScoped<RoleAssignmentService>();

        // Permissions
        services.AddScoped<AuthorizationVersionService>();
        services.AddScoped<EffectivePermissionService>();
        services.TryAddScoped<IFeatureAvailability, FeatureManagerAvailability>();
        services.AddSingleton<PermissionCatalogSync>();
        services.AddHostedService(sp => sp.GetRequiredService<PermissionCatalogSync>());

        // Auditing
        services.AddSingleton<AuditQueue>();
        services.AddSingleton<AuditWriterState>();
        services.AddScoped<IAuditWriter, AuditWriter>();
        services.AddHostedService<AuditQueueWorker>();
        services.AddScoped<AuditQueryService>();
        services.AddSingleton<AuditRetentionJob>();
        services.AddHostedService(sp => sp.GetRequiredService<AuditRetentionJob>());
        services.AddHealthChecks().AddCheck<AuditHealthCheck>("audit");

        return services;
    }

    /// <summary>Adds the ordered access checks. Call after UseAuthentication.</summary>
    public static IApplicationBuilder UseModuPOSAccessPipeline(this IApplicationBuilder app) =>
        app.UseMiddleware<AccessPipelineMiddleware>();

    /// <summary>The configured fixed-window permit count for the sign-in endpoint.</summary>
    public static int SignInRateLimit(IServiceProvider services) =>
        services.GetRequiredService<IOptions<AuthOptions>>().Value.SignInRateLimitPerMinute;
}
