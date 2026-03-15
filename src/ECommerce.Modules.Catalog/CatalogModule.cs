using ECommerce.Modules.Catalog.Domain;
using ECommerce.Modules.Catalog.Endpoints;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Routing;

namespace ECommerce.Modules.Catalog;

public static class CatalogModule
{
    public static IServiceCollection AddCatalogModule(this IServiceCollection services, Action<DbContextOptionsBuilder> configureDb)
    {
        services.AddDbContext<CatalogDbContext>(configureDb);
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<ICatalogUnitOfWork>(sp => sp.GetRequiredService<CatalogDbContext>());

        return services;
    }

    public static void MapCatalogEndpoints(this IEndpointRouteBuilder endpoints) =>
        CatalogEndpointMapper.Map(endpoints);
}
