using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.Tests.EndToEnd;

public class ECommerceWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private SqliteConnection _connection = null!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        builder.UseEnvironment("Testing");

        // Override outbox interval to 1 second for fast E2E test feedback
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:IntervalSeconds"] = "1"
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

            // Re-register with shared in-memory SQLite
            services.AddDbContext<CatalogDbContext>(o => o.UseSqlite(_connection));
            services.AddDbContext<OrderingDbContext>(o => o.UseSqlite(_connection));
            services.AddDbContext<BillingDbContext>(o => o.UseSqlite(_connection));
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var services = scope.ServiceProvider;

        var catalogCtx = services.GetRequiredService<CatalogDbContext>();
        await catalogCtx.Database.EnsureCreatedAsync();

        var orderingCtx = services.GetRequiredService<OrderingDbContext>();
        var orderingCreator = orderingCtx.GetService<IRelationalDatabaseCreator>();
        try { await orderingCreator.CreateTablesAsync(); } catch { }

        var billingCtx = services.GetRequiredService<BillingDbContext>();
        var billingCreator = billingCtx.GetService<IRelationalDatabaseCreator>();
        try { await billingCreator.CreateTablesAsync(); } catch { }
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _connection?.Dispose();
        await base.DisposeAsync();
    }
}
