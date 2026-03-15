using ECommerce.Modules.Billing.Domain;
using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Modules.Catalog.Domain;
using ECommerce.Modules.Catalog.Infrastructure;
using ECommerce.Modules.Ordering.Domain;
using ECommerce.Modules.Ordering.Infrastructure;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NSubstitute;

namespace ECommerce.Tests.Integration.Fixtures;

public sealed class DbContextFactory : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly IPublisher _publisher;

    public DbContextFactory()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _publisher = Substitute.For<IPublisher>();

        using var catalogCtx = CreateCatalogContext();
        catalogCtx.Database.EnsureCreated();

        using var orderingCtx = CreateOrderingContext();
        var orderingCreator = orderingCtx.GetService<IRelationalDatabaseCreator>();
        try { orderingCreator.CreateTables(); } catch { }

        using var billingCtx = CreateBillingContext();
        var billingCreator = billingCtx.GetService<IRelationalDatabaseCreator>();
        try { billingCreator.CreateTables(); } catch { }
    }

    public CatalogDbContext CreateCatalogContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new CatalogDbContext(options, _publisher);
    }

    public OrderingDbContext CreateOrderingContext()
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new OrderingDbContext(options, _publisher);
    }

    public BillingDbContext CreateBillingContext()
    {
        var options = new DbContextOptionsBuilder<BillingDbContext>()
            .UseSqlite(_connection)
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

    public void Dispose() => _connection.Dispose();
}
