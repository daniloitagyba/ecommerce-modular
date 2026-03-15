using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            RemoveService<DbContextOptions<CatalogDbContext>>(services);
            RemoveService<DbContextOptions<OrderingDbContext>>(services);
            RemoveService<DbContextOptions<BillingDbContext>>(services);

            // Register with shared in-memory SQLite
            services.AddDbContext<CatalogDbContext>(o => o.UseSqlite(_connection));
            services.AddDbContext<OrderingDbContext>(o => o.UseSqlite(_connection));
            services.AddDbContext<BillingDbContext>(o => o.UseSqlite(_connection));
        });
    }

    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor is not null)
            services.Remove(descriptor);
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
