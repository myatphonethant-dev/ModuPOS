using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Inventory.Services.Category;
using ModuPOS.Inventory.Services.Product;

namespace ModuPOS.Inventory;

/// <summary>
/// Feature manager and service registration for the Inventory module.
/// </summary>
public static class FeatureManager
{
    /// <summary>
    /// Registers Inventory module services into the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
