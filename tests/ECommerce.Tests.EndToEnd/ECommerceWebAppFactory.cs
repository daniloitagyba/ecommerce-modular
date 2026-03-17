using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace ECommerce.Tests.EndToEnd;

public class ECommerceWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override outbox interval to 1 second for fast E2E test feedback
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:IntervalSeconds"] = "1",
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove ALL EF Core DbContext and provider registrations
            var descriptorsToRemove = services
                .Where(d =>
                    d.ServiceType.FullName?.Contains("DbContextOptions") == true ||
                    d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true ||
                    d.ImplementationType?.FullName?.Contains("Npgsql") == true ||
                    d.ServiceType.FullName?.Contains("Npgsql") == true ||
                    d.ServiceType == typeof(CatalogDbContext) ||
                    d.ServiceType == typeof(OrderingDbContext) ||
                    d.ServiceType == typeof(BillingDbContext))
                .ToList();

            foreach (var descriptor in descriptorsToRemove)
                services.Remove(descriptor);

            var connStr = _container.GetConnectionString();
            services.AddDbContext<CatalogDbContext>(o => o.UseNpgsql(connStr));
            services.AddDbContext<OrderingDbContext>(o =>
            {
                o.UseNpgsql(connStr);
                o.AddInterceptors(new OutboxInterceptor());
            });
            services.AddDbContext<BillingDbContext>(o => o.UseNpgsql(connStr));
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Force app startup so tables are created via DatabaseInitializer
        _ = Services;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }
}
