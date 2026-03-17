using ECommerce.Modules.Catalog.Application.Queries;
using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Application;
using ECommerce.Tests.Integration.Fixtures;

namespace ECommerce.Tests.Integration.Catalog;

[Collection("Postgres")]
public class GetProductsHandlerTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private readonly DbContextFactory _factory = new(postgres.ConnectionString);

    [Fact]
    public async Task Handle_ShouldReturnAllProducts()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);

        var category = Category.Create("Electronics", "Devices").Value!;
        db.Categories.Add(category);
        db.Products.Add(Product.Create("Laptop", "LAP-1", 1000m, 10, category.Id).Value!);
        db.Products.Add(Product.Create("Mouse", "MOU-1", 50m, 100, category.Id).Value!);
        await db.SaveChangesAsync();

        var handler = new GetProductsHandler(repository);

        var result = await handler.Handle(new GetProductsQuery(new PagedRequest()), CancellationToken.None);

        result.Items.Count.Should().Be(2);
        result.TotalCount.Should().Be(2);
        result.Items.Should().Contain(p => p.Name == "Laptop" && p.CategoryName == "Electronics");
        result.Items.Should().Contain(p => p.Name == "Mouse");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoProducts()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);
        var handler = new GetProductsHandler(repository);

        var result = await handler.Handle(new GetProductsQuery(new PagedRequest()), CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetProductById_ShouldReturnProduct_WhenExists()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);

        var category = Category.Create("Electronics", "Devices").Value!;
        db.Categories.Add(category);
        var product = Product.Create("Laptop", "LAP-2", 1000m, 10, category.Id).Value!;
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var handler = new GetProductByIdHandler(repository);

        var result = await handler.Handle(new GetProductByIdQuery(product.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Laptop");
        result.Value.Sku.Should().Be("LAP-2");
    }

    [Fact]
    public async Task GetProductById_ShouldReturnFailure_WhenNotExists()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);
        var handler = new GetProductByIdHandler(repository);

        var result = await handler.Handle(new GetProductByIdQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NotFound");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
