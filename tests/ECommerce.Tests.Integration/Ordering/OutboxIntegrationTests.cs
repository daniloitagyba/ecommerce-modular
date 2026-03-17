using ECommerce.Modules.Ordering.Domain;
using ECommerce.Tests.Integration.Fixtures;

namespace ECommerce.Tests.Integration.Ordering;

[Collection("Postgres")]
public class OutboxIntegrationTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private readonly DbContextFactory _factory = new(postgres.ConnectionString);

    [Fact]
    public async Task SaveChangesAsync_ShouldConvertIntegrationEventsToOutboxMessages()
    {
        await using var db = _factory.CreateOrderingContext();

        var items = new[] { OrderItem.Create(Guid.NewGuid(), "Widget", 25m, 2) };
        var order = Order.Create("buyer@example.com", items).Value;

        db.Orders.Add(order!);
        await db.SaveChangesAsync();

        var outboxMessages = db.OutboxMessages.ToList();
        outboxMessages.Should().ContainSingle();

        var message = outboxMessages[0];
        message.Type.Should().Contain("OrderCreatedIntegrationEvent");
        message.Content.Should().Contain("buyer@example.com");
        message.ProcessedAt.Should().BeNull();
        message.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldSaveOrderAndOutboxMessage_InSameTransaction()
    {
        await using var db = _factory.CreateOrderingContext();

        var items = new[] { OrderItem.Create(Guid.NewGuid(), "Gadget", 100m, 1) };
        var order = Order.Create("acid@test.com", items).Value;

        db.Orders.Add(order!);
        await db.SaveChangesAsync();

        db.Orders.Should().ContainSingle();
        db.OutboxMessages.Should().ContainSingle();

        var message = db.OutboxMessages.Single();
        message.Content.Should().Contain(order!.Id.ToString());
    }

    [Fact]
    public async Task SaveChangesAsync_ShouldNotCreateOutboxMessage_WhenNoIntegrationEvents()
    {
        await using var db = _factory.CreateOrderingContext();

        await db.SaveChangesAsync();

        db.OutboxMessages.Should().BeEmpty();
    }

    [Fact]
    public async Task OutboxMessage_ShouldContainCorrectTotalAmount()
    {
        await using var db = _factory.CreateOrderingContext();

        var items = new[]
        {
            OrderItem.Create(Guid.NewGuid(), "Item A", 30m, 3),
            OrderItem.Create(Guid.NewGuid(), "Item B", 50m, 2)
        };

        var order = Order.Create("total@test.com", items).Value;
        db.Orders.Add(order!);
        await db.SaveChangesAsync();

        var message = db.OutboxMessages.Single();
        // Total = (30*3) + (50*2) = 190
        message.Content.Should().Contain("190");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
