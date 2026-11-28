using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Organization.Interfaces;
using ModuPOS.Organization.Services;

namespace ModuPOS.Organization;

public static class DependencyInjection
{
    public static IServiceCollection AddOrganizationModule(this IServiceCollection services)
    {
        services.AddScoped<IBranchService, BranchService>();
        services.AddScoped<IStaffService, StaffService>();
        services.AddScoped<IStaffRoleService, StaffRoleService>();
        services.AddScoped<IShiftService, ShiftService>();

        return services;
    }
}