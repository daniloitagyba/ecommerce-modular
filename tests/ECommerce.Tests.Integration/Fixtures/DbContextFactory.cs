using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using NSubstitute;

namespace ECommerce.Tests.Integration.Fixtures;

public sealed class DbContextFactory : IAsyncDisposable
{
    private readonly string _connectionString;
    private readonly IPublisher _publisher;

    public DbContextFactory(string baseConnectionString)
    {
        // Create a unique database per test class for isolation
        var dbName = $"test_{Guid.NewGuid():N}";
        _connectionString = ReplaceDatabase(baseConnectionString, dbName);
        _publisher = Substitute.For<IPublisher>();

        CreateDatabase(baseConnectionString, dbName);
        InitializeTables();
    }

    private static string ReplaceDatabase(string connectionString, string dbName)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = dbName };
        return builder.ConnectionString;
    }

    private static void CreateDatabase(string baseConnectionString, string dbName)
    {
        using var conn = new NpgsqlConnection(baseConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"CREATE DATABASE \"{dbName}\"";
        cmd.ExecuteNonQuery();
    }

    private void InitializeTables()
    {
        using var catalogCtx = CreateCatalogContext();
        catalogCtx.Database.EnsureCreated();

        using var orderingCtx = CreateOrderingContext();
        orderingCtx.GetService<IRelationalDatabaseCreator>().CreateTables();

        using var billingCtx = CreateBillingContext();
        billingCtx.GetService<IRelationalDatabaseCreator>().CreateTables();
    }

    public CatalogDbContext CreateCatalogContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new CatalogDbContext(options, _publisher);
    }

    public OrderingDbContext CreateOrderingContext()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(_connectionString)
            .AddInterceptors(new OutboxInterceptor())
            .Options;
        return new OrderingDbContext(options, _publisher);
    }

    public BillingDbContext CreateBillingContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseNpgsql(_connectionString)
            .Options;
        return new BillingDbContext(options, _publisher);
    }

    // Catalog repositories
    public ProductRepository CreateProductRepository(CatalogDbContext context) => new(context);
    public CategoryRepository CreateCategoryRepository(CatalogDbContext context) => new(context);

    // Ordering repositories
    public OrderRepository CreateOrderRepository(OrderingDbContext context) => new(context);

    // Billing repositories
    public PaymentRepository CreatePaymentRepository(BillingDbContext context) => new(context);
    public InvoiceRepository CreateInvoiceRepository(BillingDbContext context) => new(context);

    public IPublisher Publisher => _publisher;

    public async ValueTask DisposeAsync()
    {
        // Drop the test database to clean up
        var builder = new NpgsqlConnectionStringBuilder(_connectionString);
        var dbName = builder.Database;
        builder.Database = "postgres";

        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();

        // Terminate existing connections before dropping
        await using var terminate = conn.CreateCommand();
        terminate.CommandText = $"""
            SELECT pg_terminate_backend(pid)
            FROM pg_stat_activity
            WHERE datname = '{dbName}' AND pid <> pg_backend_pid()
            """;
        await terminate.ExecuteNonQueryAsync();

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $"DROP DATABASE IF EXISTS \"{dbName}\"";
        await cmd.ExecuteNonQueryAsync();
    }
}
