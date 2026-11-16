using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Auth.Services.Auth;
using ModuPOS.Auth.Services.Permission;
using ModuPOS.Auth.Services.Role;
using ModuPOS.Auth.Services.User;

namespace ModuPOS.Auth;

public static class DependencyInjection
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
