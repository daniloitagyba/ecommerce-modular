using ECommerce.Modules.Ordering.Domain;
using ECommerce.Modules.Ordering.Endpoints;
using ECommerce.Modules.Ordering.Infrastructure;
using ECommerce.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.Modules.Ordering;

public static class OrderingModule
{
    public static IServiceCollection AddOrderingModule(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddSingleton<OutboxInterceptor>();
        services.AddDbContext<OrderingDbContext>((sp, options) =>
        {
            configureDb(options);
            options.AddInterceptors(sp.GetRequiredService<OutboxInterceptor>());
        });
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderingUnitOfWork>(sp => sp.GetRequiredService<OrderingDbContext>());

        return services;
    }

    public static void MapOrderingEndpoints(this IEndpointRouteBuilder endpoints) =>
        OrderingEndpointMapper.Map(endpoints);
}
