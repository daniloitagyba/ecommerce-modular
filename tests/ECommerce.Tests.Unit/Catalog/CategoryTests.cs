using ECommerce.Modules.Catalog.Domain;

namespace ECommerce.Tests.Unit.Catalog;

public class CategoryTests
{
    [Fact]
    public void Create_ShouldSucceedWithValidInput()
    {
        var result = Category.Create("Electronics", "Gadgets and devices");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Electronics");
        result.Value.Description.Should().Be("Gadgets and devices");
        result.Value.Products.Should().BeEmpty();
    }

    [Fact]
    public void Create_ShouldFail_WhenNameIsEmpty()
    {
        var result = Category.Create("", "Desc");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Category.EmptyName");
    }

    [Fact]
    public void Update_ShouldChangeNameAndDescription()
    {
        var category = Category.Create("Old", "Old desc").Value!;

        category.Update("New", "New desc");

        category.Name.Should().Be("New");
        category.Description.Should().Be("New desc");
    }
}
