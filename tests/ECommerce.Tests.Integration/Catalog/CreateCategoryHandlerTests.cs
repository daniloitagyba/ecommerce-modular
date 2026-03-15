using ECommerce.Modules.Catalog.Application.Commands;
using ECommerce.Tests.Integration.Fixtures;

namespace ECommerce.Tests.Integration.Catalog;

public class CreateCategoryHandlerTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    [Fact]
    public async Task Handle_ShouldCreateCategoryInDatabase()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateCategoryRepository(db);
        var handler = new CreateCategoryHandler(repository, db);
        var command = new CreateCategoryCommand("Electronics", "Gadgets and devices");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        var category = await db.Categories.FindAsync(result.Value);
        category.Should().NotBeNull();
        category!.Name.Should().Be("Electronics");
        category.Description.Should().Be("Gadgets and devices");
    }

    public void Dispose() => _factory.Dispose();
}
