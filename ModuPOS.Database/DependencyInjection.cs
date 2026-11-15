using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModuPOS.FeatureManagement.Interfaces;
using ModuPOS.Shared.Tenancy;

namespace ModuPOS.Database;

public static class DependencyInjection
{
    public static IServiceCollection AddModuPOSDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read lazily so test hosts can override the connection string.
        services.AddDbContext<ModuPOSDbContext>((sp, o) =>
        {
            var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Default");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("ConnectionStrings:Default is not configured.");

            o.UseSqlServer(connectionString);
        });

        // Modules depend on the EF base type only, so they need no reference to this project.
        services.AddScoped<DbContext>(sp => sp.GetRequiredService<ModuPOSDbContext>());
        services.AddScoped<ICustomerFeatureRepository, CustomerFeatureRepository>();

        return services;
    }
}

/// <summary>Used by <c>dotnet ef</c>. Override the connection with MODUPOS_DESIGN_SQL if needed.</summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ModuPOSDbContext>
{
    public ModuPOSDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("MODUPOS_DESIGN_SQL")
            ?? @"Server=(localdb)\MSSQLLocalDB;Database=ModuPOS_Dev;Trusted_Connection=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ModuPOSDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new ModuPOSDbContext(options, new TenantContext());
    }
}
