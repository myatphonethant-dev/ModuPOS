using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModuPOS.LicenseManagement.Interfaces;
using ModuPOS.LicenseManagement.Models;
using ModuPOS.LicenseManagement.Services;

namespace ModuPOS.LicenseManagement;

/// <summary>
/// Feature manager and service registration for the LicenseManagement module.
/// </summary>
public static class FeatureManager
{
    /// <summary>
    /// Registers LicenseManagement module services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddLicenseManagement(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<LicenseValidationOptions>(
            configuration.GetSection(
                LicenseValidationOptions.SectionName));

        services.AddSingleton<ILicenseValidator, LicenseValidator>();

        services.AddSingleton<ILicenseProvider, LicenseProvider>();

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<ITenantLicenseStatus, TenantLicenseStatus>();

        return services;
    }
}
