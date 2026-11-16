namespace ModuPOS.FeatureManagement;

using Microsoft.Extensions.DependencyInjection;
using ModuPOS.FeatureManagement.Features.CustomerFeatures;
using ModuPOS.FeatureManagement.Interfaces;
using ModuPOS.FeatureManagement.Services;

public static class FeatureManager
{
    public static IServiceCollection AddFeatureManagement(
        this IServiceCollection services)
    {
        services.AddScoped<CustomerFeatureService>();
        services.AddScoped<IFeatureManager, Services.FeatureManager>();

        return services;
    }
}
