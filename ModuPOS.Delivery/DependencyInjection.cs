using Microsoft.Extensions.DependencyInjection;
using ModuPOS.Delivery.Interfaces;
using ModuPOS.Delivery.Services;

namespace ModuPOS.Delivery;

public static class DependencyInjection
{
    public static IServiceCollection AddDeliveryModule(this IServiceCollection services)
    {
        services.AddScoped<ICustomerOrderService, CustomerOrderService>();

        return services;
    }
}