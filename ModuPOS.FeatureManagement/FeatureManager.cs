using Microsoft.Extensions.DependencyInjection;
using ModuPOS.FeatureManagement.Features.CustomerFeatures;
using ModuPOS.FeatureManagement.Interfaces;
using ModuPOS.FeatureManagement.Services;

namespace ModuPOS.FeatureManagement;

/// <summary>
/// Feature manager and service registration for the FeatureManagement module.
/// </summary>
public static class FeatureManager
{
    /// <summary>
    /// Registers FeatureManagement module services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddFeatureManagement(
        this IServiceCollection services)
    {
        services.AddScoped<CustomerFeatureService>();
        services.AddScoped<IFeatureManager, Services.FeatureManager>();

        return services;
    }
}
