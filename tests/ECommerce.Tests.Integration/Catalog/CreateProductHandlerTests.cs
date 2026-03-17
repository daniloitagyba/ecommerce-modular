using ECommerce.Modules.Catalog.Application.Commands;
using ECommerce.Modules.Catalog.Domain;
using ECommerce.Tests.Integration.Fixtures;

namespace ECommerce.Tests.Integration.Catalog;

[Collection("Postgres")]
public class CreateProductHandlerTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private readonly DbContextFactory _factory = new(postgres.ConnectionString);

    [Fact]
    public async Task Handle_ShouldCreateProductInDatabase()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateProductRepository(db);

        // Seed category
        var category = Category.Create("Electronics", "Devices").Value!;
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var handler = new CreateProductHandler(repository, db);
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, 50, category.Id);

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        var product = await db.Products.FindAsync(result.Value);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Laptop");
        product.Sku.Should().Be("LAP-001");
        product.Price.Should().Be(999.99m);
        product.StockQuantity.Should().Be(50);
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
