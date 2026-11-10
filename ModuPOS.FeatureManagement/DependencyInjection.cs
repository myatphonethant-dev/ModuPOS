using Microsoft.Extensions.DependencyInjection;
using ModuPOS.FeatureManagement.Interfaces;
using ModuPOS.FeatureManagement.Services;

namespace ModuPOS.FeatureManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddFeatureManagement(
        this IServiceCollection services)
    {
        services.AddScoped<IFeatureManager, FeatureManager>();

        return services;
    }
}