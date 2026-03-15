using ECommerce.Modules.Ordering.Application.Commands;
using FluentValidation.TestHelper;

namespace ECommerce.Tests.Unit.Ordering;

public class PlaceOrderValidatorTests
{
    private readonly PlaceOrderValidator _validator = new();

    [Fact]
    public void ShouldPass_WhenValid()
    {
        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemDto(Guid.NewGuid(), "Laptop", 999.99m, 1)
        ]);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenEmailIsEmpty()
    {
        var command = new PlaceOrderCommand("",
        [
            new OrderItemDto(Guid.NewGuid(), "Laptop", 999.99m, 1)
        ]);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CustomerEmail);
    }

    [Fact]
    public void ShouldFail_WhenEmailIsInvalid()
    {
        var command = new PlaceOrderCommand("not-an-email",
        [
            new OrderItemDto(Guid.NewGuid(), "Laptop", 999.99m, 1)
        ]);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CustomerEmail);
    }

    [Fact]
    public void ShouldFail_WhenLinesAreEmpty()
    {
        var command = new PlaceOrderCommand("john@example.com", []);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Lines);
    }

    [Fact]
    public void ShouldFail_WhenLineProductIdIsEmpty()
    {
        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemDto(Guid.Empty, "Laptop", 999.99m, 1)
        ]);

        var result = _validator.TestValidate(command);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void ShouldFail_WhenLineQuantityIsZero()
    {
        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemDto(Guid.NewGuid(), "Laptop", 999.99m, 0)
        ]);

        var result = _validator.TestValidate(command);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void ShouldFail_WhenLinePriceIsZero()
    {
        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemDto(Guid.NewGuid(), "Laptop", 0m, 1)
        ]);

        var result = _validator.TestValidate(command);
        result.ShouldHaveAnyValidationError();
    }
}
