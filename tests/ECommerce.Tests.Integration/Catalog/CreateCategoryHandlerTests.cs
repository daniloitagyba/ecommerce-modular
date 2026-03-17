using ECommerce.Modules.Catalog.Application.Commands;
using ECommerce.Tests.Integration.Fixtures;

namespace ECommerce.Tests.Integration.Catalog;

[Collection("Postgres")]
public class CreateCategoryHandlerTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private readonly DbContextFactory _factory = new(postgres.ConnectionString);

    [Fact]
    public async Task Handle_ShouldCreateCategoryInDatabase()
    {
        await using var db = _factory.CreateCatalogContext();
        var repository = _factory.CreateCategoryRepository(db);
        var handler = new CreateCategoryHandler(repository, db);
        var command = new CreateCategoryCommand("Electronics", "Gadgets and devices");

        var result = await handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        var category = await db.Categories.FindAsync(result.Value);
        category.Should().NotBeNull();
        category!.Name.Should().Be("Electronics");
        category.Description.Should().Be("Gadgets and devices");
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public async Task DisposeAsync() => await _factory.DisposeAsync();
}
