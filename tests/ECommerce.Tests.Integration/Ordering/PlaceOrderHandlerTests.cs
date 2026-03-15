using ECommerce.Modules.Ordering.Application.Commands;
using ECommerce.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Tests.Integration.Ordering;

public class PlaceOrderHandlerTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    [Fact]
    public async Task Handle_ShouldCreateOrderWithLines()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);
        var handler = new PlaceOrderHandler(repository, db);

        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemDto(Guid.NewGuid(), "Laptop", 999.99m, 2),
            new OrderItemDto(Guid.NewGuid(), "Mouse", 50m, 1)
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == result.Value);
        order.CustomerEmail.Should().Be("john@example.com");
        order.Lines.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldPersistOrderLineDetails()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);
        var handler = new PlaceOrderHandler(repository, db);
        var productId = Guid.NewGuid();

        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemDto(productId, "Laptop", 999.99m, 3)
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        var order = await db.Orders.Include(o => o.Lines).FirstAsync(o => o.Id == result.Value);
        var line = order.Lines.Single();
        line.ProductId.Should().Be(productId);
        line.ProductName.Should().Be("Laptop");
        line.UnitPrice.Should().Be(999.99m);
        line.Quantity.Should().Be(3);
    }

    public void Dispose() => _factory.Dispose();
}
