using ECommerce.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Integration.Ordering;

[Collection("Postgres")]
public class OrderSchemaTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private readonly DbContextFactory _factory = new(postgres.ConnectionString);

    [Fact]
    public async Task OrderingSchema_ShouldHaveNoModelDifferences()
    {
        await using var db = _factory.CreateOrderingContext();

        // GenerateCreateScript produces the SQL that EF expects.
        // If CreateTables already ran, any mismatch means the DB
        // is out of sync with the model — exactly the bug we hit
        // when TotalAmount was missing from the Orders table.
        var pendingChanges = db.Database.GenerateCreateScript();

        // Verify we can insert and read back an order with all mapped columns
        var order = ECommerce.Modules.Ordering.Domain.Order.Create(
            "schema@test.com",
            [ECommerce.Modules.Ordering.Domain.OrderItem.Create(Guid.NewGuid(), "Item", 10m, 1)]
        ).Value;

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        await using var freshDb = _factory.CreateOrderingContext();
        var loaded = await freshDb.Orders.Include(o => o.Items).FirstAsync(o => o.Id == order.Id);

        // If any mapped property fails to round-trip, the schema is wrong
        loaded.CustomerEmail.Should().Be("schema@test.com");
        loaded.TotalAmount.Should().Be(10m);
        loaded.Status.Should().Be(ECommerce.Modules.Ordering.Domain.OrderStatus.Pending);
        loaded.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        loaded.Items.Should().ContainSingle();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
