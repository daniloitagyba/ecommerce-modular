using ECommerce.Modules.Catalog.Application.Commands;
using ECommerce.Modules.Catalog.Domain;
using ECommerce.Tests.Integration.Fixtures;

namespace ECommerce.Tests.Integration.Catalog;

[Collection("Postgres")]
public class UpdateStockHandlerTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private readonly DbContextFactory _factory = new(postgres.ConnectionString);

    [Fact]
    public async Task Handle_ShouldIncreaseStock()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);

        var category = Category.Create("Cat", "Desc").Value!;
        db.Categories.Add(category);
        var product = Product.Create("P", "S-1", 10m, 20, category.Id).Value!;
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateStockHandler(repository, db);

        var result = await handler.Handle(new UpdateStockCommand(product.Id, 10), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Products.FindAsync(product.Id);
        updated!.StockQuantity.Should().Be(30);
    }

    [Fact]
    public async Task Handle_ShouldDecreaseStock()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);

        var category = Category.Create("Cat", "Desc").Value!;
        db.Categories.Add(category);
        var product = Product.Create("P", "S-2", 10m, 20, category.Id).Value!;
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateStockHandler(repository, db);

        var result = await handler.Handle(new UpdateStockCommand(product.Id, -5), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var updated = await db.Products.FindAsync(product.Id);
        updated!.StockQuantity.Should().Be(15);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenProductNotFound()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);
        var handler = new UpdateStockHandler(repository, db);

        var result = await handler.Handle(new UpdateStockCommand(Guid.NewGuid(), 10), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenInsufficientStock()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);

        var category = Category.Create("Cat", "Desc").Value!;
        db.Categories.Add(category);
        var product = Product.Create("P", "S-3", 10m, 5, category.Id).Value!;
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new UpdateStockHandler(repository, db);

        var result = await handler.Handle(new UpdateStockCommand(product.Id, -10), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.InsufficientStock");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
