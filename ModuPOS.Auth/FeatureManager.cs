namespace ModuPOS.Auth;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Auth.Interfaces;
using ModuPOS.Auth.Services;

public static class FeatureManager
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
