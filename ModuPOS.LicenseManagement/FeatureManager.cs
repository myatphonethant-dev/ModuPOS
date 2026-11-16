namespace ModuPOS.LicenseManagement;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ModuPOS.LicenseManagement.Interfaces;
using ModuPOS.LicenseManagement.Models;
using ModuPOS.LicenseManagement.Services;

public static class FeatureManager
{
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
