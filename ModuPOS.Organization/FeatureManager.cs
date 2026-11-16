using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Organization.Services.Branch;
using ModuPOS.Organization.Services.Shift;
using ModuPOS.Organization.Services.Staff;

namespace ModuPOS.Organization;

/// <summary>
/// Feature manager and service registration for the Organization module.
/// </summary>
public static class FeatureManager
{
    /// <summary>
    /// Registers Organization module services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IStaffRoleService, StaffRoleService>();
        services.AddScoped<IShiftService, ShiftService>();

        return services;
    }
}
