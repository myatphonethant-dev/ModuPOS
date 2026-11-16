using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Auth.Services.Auth;
using ModuPOS.Auth.Services.Permission;
using ModuPOS.Auth.Services.Role;
using ModuPOS.Auth.Services.User;

namespace ModuPOS.Auth;

/// <summary>
/// Feature manager and service registration for the Auth module.
/// </summary>
public static class FeatureManager
{
    /// <summary>
    /// Registers Auth module services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
