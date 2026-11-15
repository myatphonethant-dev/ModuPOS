using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Organization.Services;

namespace ModuPOS.Organization;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganization(this IServiceCollection services)
    {
        services.AddScoped<IBranchAccessService, BranchAccessService>();
        return services;
    }
}
