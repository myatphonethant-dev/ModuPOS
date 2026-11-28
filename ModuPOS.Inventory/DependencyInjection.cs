using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Inventory.Interfaces;
using ModuPOS.Inventory.Services;

namespace ModuPOS.Inventory;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryModule(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}