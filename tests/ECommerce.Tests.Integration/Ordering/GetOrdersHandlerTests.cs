using ECommerce.Modules.Ordering.Application.Commands;
using ECommerce.Modules.Ordering.Application.Queries;
using ECommerce.Shared.Application;
using ECommerce.Shared.Domain;
using ECommerce.Tests.Integration.Fixtures;
using NSubstitute;

namespace ECommerce.Tests.Integration.Ordering;

[Collection("Postgres")]
public class GetOrdersHandlerTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private readonly DbContextFactory _factory = new(postgres.ConnectionString);

    private static IProductChecker CreateMockProductChecker(params (Guid Id, string Name, decimal Price, int Qty)[] products)
    {
        var checker = Substitute.For<IProductChecker>();
        checker.ValidateProductsAsync(Arg.Any<IReadOnlyList<ProductLineRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ValidatedProduct>>.Success(
                products.Select(p => new ValidatedProduct(p.Id, p.Name, p.Price, p.Qty)).ToList()));
        return checker;
    }

    [Fact]
    public async Task Handle_ShouldReturnAllOrders()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var placeHandler1 = new PlaceOrderHandler(repository, db,
            CreateMockProductChecker((id1, "P1", 10m, 1)));
        await placeHandler1.Handle(new PlaceOrderCommand("a@b.com",
            [new OrderItemRequest(id1, 1)]), CancellationToken.None);

        var placeHandler2 = new PlaceOrderHandler(repository, db,
            CreateMockProductChecker((id2, "P2", 20m, 2)));
        await placeHandler2.Handle(new PlaceOrderCommand("c@d.com",
            [new OrderItemRequest(id2, 2)]), CancellationToken.None);

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
        var productId = Guid.NewGuid();

        var placeHandler = new PlaceOrderHandler(repository, db,
            CreateMockProductChecker((productId, "Laptop", 999m, 1)));
        var orderResult = await placeHandler.Handle(new PlaceOrderCommand("a@b.com",
            [new OrderItemRequest(productId, 1)]), CancellationToken.None);

        var handler = new GetOrderByIdHandler(repository);

        var result = await handler.Handle(new GetOrderByIdQuery(orderResult.Value), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerEmail.Should().Be("a@b.com");
        result.Value.Items.Should().ContainSingle().Which.ProductName.Should().Be("Laptop");
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

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
