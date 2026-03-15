using ECommerce.Modules.Ordering.Application.Commands;
using ECommerce.Modules.Ordering.Application.Queries;
using ECommerce.Shared.Application;
using ECommerce.Tests.Integration.Fixtures;

namespace ECommerce.Tests.Integration.Ordering;

public class GetOrdersHandlerTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    [Fact]
    public async Task Handle_ShouldReturnAllOrders()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);

        // Create two orders
        var placeHandler = new PlaceOrderHandler(repository, db);
        await placeHandler.Handle(new PlaceOrderCommand("a@b.com",
            [new OrderLineDto(Guid.NewGuid(), "P1", 10m, 1)]), CancellationToken.None);
        await placeHandler.Handle(new PlaceOrderCommand("c@d.com",
            [new OrderLineDto(Guid.NewGuid(), "P2", 20m, 2)]), CancellationToken.None);

        var handler = new GetOrdersHandler(repository);

        var result = await handler.Handle(new GetOrdersQuery(new PagedRequest()), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().Contain(o => o.CustomerEmail == "a@b.com");
        result.Items.Should().Contain(o => o.CustomerEmail == "c@d.com");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoOrders()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);
        var handler = new GetOrdersHandler(repository);

        var result = await handler.Handle(new GetOrdersQuery(new PagedRequest()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnOrder_WhenExists()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);
        var placeHandler = new PlaceOrderHandler(repository, db);
        var orderResult = await placeHandler.Handle(new PlaceOrderCommand("a@b.com",
            [new OrderLineDto(Guid.NewGuid(), "Laptop", 999m, 1)]), CancellationToken.None);

        var handler = new GetOrderByIdHandler(repository);

        var result = await handler.Handle(new GetOrderByIdQuery(orderResult.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomerEmail.Should().Be("a@b.com");
        result.Value.Lines.Should().ContainSingle().Which.ProductName.Should().Be("Laptop");
    }

    [Fact]
    public async Task GetOrderById_ShouldReturnFailure_WhenNotExists()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);
        var handler = new GetOrderByIdHandler(repository);

        var result = await handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotFound");
    }

    public void Dispose() => _factory.Dispose();
}
