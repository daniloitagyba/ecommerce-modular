using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace ECommerce.API;

public static class DatabaseInitializer
{
    private static readonly string[] Schemas = ["catalog", "ordering", "billing"];

    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;

        var catalogCtx = services.GetRequiredService<CatalogDbContext>();
        var databaseCreator = catalogCtx.GetService<IRelationalDatabaseCreator>();

        if (!await databaseCreator.ExistsAsync())
            await databaseCreator.CreateAsync();

        // Drop and recreate all module schemas so that table definitions
        // always match the current EF model. This is safe for development
        // because seed data is re-created on every startup.
        await DropSchemasAsync(catalogCtx);

        // Multiple DbContexts share the same PostgreSQL database.
        // We use RelationalDatabaseCreator to create tables for each context.
        await CreateTablesAsync(services.GetRequiredService<CatalogDbContext>());
        await CreateTablesAsync(services.GetRequiredService<OrderingDbContext>());
        await CreateTablesAsync(services.GetRequiredService<BillingDbContext>());
    }

    #pragma warning disable EF1003 // Schema names are hard-coded constants, not user input
    private static async Task DropSchemasAsync(DbContext context)
    {
        foreach (var schema in Schemas)
        {
            await context.Database.ExecuteSqlRawAsync(
                "DROP SCHEMA IF EXISTS \"" + schema + "\" CASCADE");
        }
    }
    #pragma warning restore EF1003

    private static async Task CreateTablesAsync(DbContext context)
    {
        var creator = context.GetService<IRelationalDatabaseCreator>();
        await creator.CreateTablesAsync();
    }
}
