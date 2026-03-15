using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.API;

public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        // For SQLite with multiple DbContexts sharing one file,
        // we use RelationalDatabaseCreator to create tables for each context.
        await EnsureTablesCreatedAsync(services.GetRequiredService<CatalogDbContext>());
        await EnsureTablesCreatedAsync(services.GetRequiredService<OrderingDbContext>());
        await EnsureTablesCreatedAsync(services.GetRequiredService<BillingDbContext>());
    }

    private static async Task EnsureTablesCreatedAsync(DbContext context)
    {
        var databaseCreator = context.GetService<IRelationalDatabaseCreator>();
        await databaseCreator.EnsureCreatedAsync();

        // CreateTables will throw if tables already exist, so we catch and ignore
        try
        {
            await databaseCreator.CreateTablesAsync();
        }
        catch (Exception)
        {
            // Tables already exist — expected for subsequent contexts sharing the same DB file
        }
    }
}
