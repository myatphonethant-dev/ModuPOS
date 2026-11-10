using Microsoft.Extensions.DependencyInjection;
using ModuPOS.FeatureManagement.Features.CustomerFeatures;
using ModuPOS.FeatureManagement.Interfaces;
using ModuPOS.FeatureManagement.Services;

namespace ModuPOS.FeatureManagement;

public static class DependencyInjection
{
    public static IServiceCollection AddFeatureManagement(
        this IServiceCollection services)
    {
        services.AddScoped<CustomerFeatureService>();
        services.AddScoped<IFeatureManager, FeatureManager>();

        return services;
    }
}