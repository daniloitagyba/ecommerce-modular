using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Domain;

namespace ECommerce.Tests.Unit.Catalog;

public class ProductTests
{
    [Fact]
    public void Create_ShouldSucceedWithValidInput()
    {
        var result = Product.Create("Laptop", "LAP-001", 999.99m, 50, Guid.NewGuid());

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Laptop");
        result.Value.Sku.Should().Be("LAP-001");
        result.Value.Price.Should().Be(999.99m);
        result.Value.StockQuantity.Should().Be(50);
    }

    [Fact]
    public void Create_ShouldFail_WhenNameIsEmpty()
    {
        var result = Product.Create("", "SKU-1", 10m, 1, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.EmptyName");
    }

    [Fact]
    public void Create_ShouldFail_WhenSkuIsInvalid()
    {
        var result = Product.Create("P", "INVALID SKU!!", 10m, 1, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sku.InvalidFormat");
    }

    [Fact]
    public void Create_ShouldFail_WhenPriceIsNegative()
    {
        var result = Product.Create("P", "SKU-1", -10m, 1, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.NegativeAmount");
    }

    [Fact]
    public void Create_ShouldFail_WhenStockIsNegative()
    {
        var result = Product.Create("P", "SKU-1", 10m, -1, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.NegativeStock");
    }

    [Fact]
    public void Create_ShouldFail_WhenCategoryIsEmpty()
    {
        var result = Product.Create("P", "SKU-1", 10m, 1, Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.EmptyCategory");
    }

    [Fact]
    public void Update_ShouldChangeNameAndPrice()
    {
        var product = Product.Create("Old Name", "SKU-1", 100m, 10, Guid.NewGuid()).Value!;

        product.Update("New Name", 200m);

        product.Name.Should().Be("New Name");
        product.Price.Should().Be(200m);
    }

    [Theory]
    [InlineData(10, 5, true)]
    [InlineData(10, 10, true)]
    [InlineData(10, 11, false)]
    [InlineData(0, 1, false)]
    public void HasStock_ShouldReturnCorrectResult(int stock, int requested, bool expected)
    {
        var product = Product.Create("P", "SKU-1", 10m, stock, Guid.NewGuid()).Value!;

        product.HasStock(requested).Should().Be(expected);
    }

    [Fact]
    public void DecreaseStock_ShouldSucceed_WhenSufficientStock()
    {
        var product = Product.Create("P", "SKU-1", 10m, 50, Guid.NewGuid()).Value!;

        var result = product.DecreaseStock(30);

        result.IsSuccess.Should().BeTrue();
        product.StockQuantity.Should().Be(20);
    }

    [Fact]
    public void DecreaseStock_ShouldFail_WhenInsufficientStock()
    {
        var product = Product.Create("Laptop", "SKU-1", 10m, 5, Guid.NewGuid()).Value!;

        var result = product.DecreaseStock(10);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Product.InsufficientStock");
    }

    [Fact]
    public void IncreaseStock_ShouldAddQuantity()
    {
        var product = Product.Create("P", "SKU-1", 10m, 10, Guid.NewGuid()).Value!;

        product.IncreaseStock(25);

        product.StockQuantity.Should().Be(35);
    }
}
