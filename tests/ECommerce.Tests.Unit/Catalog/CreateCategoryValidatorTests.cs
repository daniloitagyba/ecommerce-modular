using ECommerce.Modules.Catalog.Application.Commands;
using FluentValidation.TestHelper;

namespace ECommerce.Tests.Unit.Catalog;

public class CreateCategoryValidatorTests
{
    private readonly CreateCategoryValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var command = new CreateCategoryCommand("Electronics", "Devices");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenNameIsEmpty()
    {
        var command = new CreateCategoryCommand("", "Devices");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldFail_WhenNameExceedsMaxLength()
    {
        var command = new CreateCategoryCommand(new string('A', 201), "Devices");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void ShouldFail_WhenDescriptionExceedsMaxLength()
    {
        var command = new CreateCategoryCommand("Valid", new string('A', 501));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }
}
