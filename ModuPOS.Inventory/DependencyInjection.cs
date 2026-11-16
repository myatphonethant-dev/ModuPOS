using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Inventory.Services.Category;
using ModuPOS.Inventory.Services.Product;

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
