using ECommerce.Modules.Ordering.Application.Commands;
using ECommerce.Shared.Domain;
using ECommerce.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ECommerce.Tests.Integration.Ordering;

[Collection("Postgres")]
public class PlaceOrderHandlerTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private readonly DbContextFactory _factory = new(postgres.ConnectionString);

    [Fact]
    public async Task Handle_ShouldCreateOrderWithItems()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);

        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var productChecker = Substitute.For<IProductChecker>();
        productChecker.ValidateProductsAsync(Arg.Any<IReadOnlyList<ProductLineRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ValidatedProduct>>.Success(new List<ValidatedProduct>
            {
                new(productId1, "Laptop", 999.99m, 2),
                new(productId2, "Mouse", 50m, 1)
            }));

        var handler = new PlaceOrderHandler(repository, db, productChecker);

        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemRequest(productId1, 2),
            new OrderItemRequest(productId2, 1)
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        var order = await db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == result.Value);
        order.CustomerEmail.Should().Be("john@example.com");
        order.Items.Count.Should().Be(2);
        order.TotalAmount.Should().Be(999.99m * 2 + 50m * 1);
    }

    [Fact]
    public async Task Handle_ShouldPersistOrderItemDetails()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);
        var productId = Guid.NewGuid();

        var productChecker = Substitute.For<IProductChecker>();
        productChecker.ValidateProductsAsync(Arg.Any<IReadOnlyList<ProductLineRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ValidatedProduct>>.Success(new List<ValidatedProduct>
            {
                new(productId, "Laptop", 999.99m, 3)
            }));

        var handler = new PlaceOrderHandler(repository, db, productChecker);

        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemRequest(productId, 3)
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        var order = await db.Orders.Include(o => o.Items).FirstAsync(o => o.Id == result.Value);
        var item = order.Items.Single();
        item.ProductId.Should().Be(productId);
        item.ProductName.Should().Be("Laptop");
        item.UnitPrice.Should().Be(999.99m);
        item.Quantity.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldPersistTotalAmount()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);
        var productId = Guid.NewGuid();

        var productChecker = Substitute.For<IProductChecker>();
        productChecker.ValidateProductsAsync(Arg.Any<IReadOnlyList<ProductLineRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ValidatedProduct>>.Success(new List<ValidatedProduct>
            {
                new(productId, "Monitor", 450.00m, 2)
            }));

        var handler = new PlaceOrderHandler(repository, db, productChecker);

        var command = new PlaceOrderCommand("total@test.com",
        [
            new OrderItemRequest(productId, 2)
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        // Read from a fresh context to ensure the value comes from the database
        await using var freshDb = _factory.CreateOrderingContext();
        var persisted = await freshDb.Orders.FirstAsync(o => o.Id == result.Value);
        persisted.TotalAmount.Should().Be(900.00m);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenProductValidationFails()
    {
        await using var db = _factory.CreateOrderingContext();
        var repository = _factory.CreateOrderRepository(db);

        var productChecker = Substitute.For<IProductChecker>();
        productChecker.ValidateProductsAsync(Arg.Any<IReadOnlyList<ProductLineRequest>>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyList<ValidatedProduct>>.Failure(
                new Error("Product.NotFound", "Product not found.")));

        var handler = new PlaceOrderHandler(repository, db, productChecker);

        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemRequest(Guid.NewGuid(), 1)
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
