using ECommerce.Modules.Catalog.Application.Commands;
using FluentValidation.TestHelper;

namespace ECommerce.Tests.Unit.Catalog;

public class UpdateStockValidatorTests
{
    private readonly UpdateStockValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var command = new UpdateStockCommand(Guid.NewGuid(), 10);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenProductIdIsEmpty()
    {
        var command = new UpdateStockCommand(Guid.Empty, 10);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.ProductId);
    }
}
