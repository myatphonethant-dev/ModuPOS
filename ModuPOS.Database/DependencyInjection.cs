using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Database.AppDbContextModels;

namespace ModuPOS.Database;

public static class DependencyInjection
{
    public static IServiceCollection AddModuPOSDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Default")), 
            ServiceLifetime.Transient, 
            ServiceLifetime.Transient);

        return services;
    }
}
