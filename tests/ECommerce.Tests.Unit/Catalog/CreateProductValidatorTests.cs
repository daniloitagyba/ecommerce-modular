using ECommerce.Modules.Catalog.Application.Commands;
using FluentValidation.TestHelper;

namespace ECommerce.Tests.Unit.Catalog;

public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var command = new CreateProductCommand("Laptop", "LAP-001", 999.99m, 50, Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenNameIsEmpty()
    {
        var command = new CreateProductCommand("", "SKU", 10m, 1, Guid.NewGuid());

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldFail_WhenSkuIsEmpty()
    {
        var command = new CreateProductCommand("P", "", 10m, 1, Guid.NewGuid());

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Sku);
    }

    [Fact]
    public void ShouldFail_WhenPriceIsZeroOrNegative()
    {
        var command = new CreateProductCommand("P", "S", 0m, 1, Guid.NewGuid());

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void ShouldFail_WhenStockIsNegative()
    {
        var command = new CreateProductCommand("P", "S", 10m, -1, Guid.NewGuid());

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.StockQuantity);
    }

    [Fact]
    public void ShouldFail_WhenCategoryIdIsEmpty()
    {
        var command = new CreateProductCommand("P", "S", 10m, 1, Guid.Empty);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CategoryId);
    }
}
