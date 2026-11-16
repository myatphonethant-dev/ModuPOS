namespace ModuPOS.Inventory;

using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Inventory.Interfaces;
using ModuPOS.Inventory.Services;

public static class FeatureManager
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
