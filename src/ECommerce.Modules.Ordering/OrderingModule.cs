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
        services.AddDbContext<OrderingDbContext>(configureDb);
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderingUnitOfWork>(sp => sp.GetRequiredService<OrderingDbContext>());

        return services;
    }

    public static void MapOrderingEndpoints(this IEndpointRouteBuilder endpoints) =>
        OrderingEndpointMapper.Map(endpoints);
}
