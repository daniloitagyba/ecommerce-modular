using ECommerce.Modules.Billing.Endpoints;
using ECommerce.Modules.Billing.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.Modules.Billing;

public static class BillingModule
{
    public static IServiceCollection AddBillingModule(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<BillingDbContext>(configureDb);
        return services;
    }

    public static void MapBillingEndpoints(this IEndpointRouteBuilder endpoints) =>
        BillingEndpointMapper.Map(endpoints);
}
