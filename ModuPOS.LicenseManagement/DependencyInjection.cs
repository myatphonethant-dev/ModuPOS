using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.LicenseManagement.Interfaces;
using ModuPOS.LicenseManagement.Models;
using ModuPOS.LicenseManagement.Services;

namespace ModuPOS.LicenseManagement;

public static class DependencyInjection
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

        return services;
    }
}