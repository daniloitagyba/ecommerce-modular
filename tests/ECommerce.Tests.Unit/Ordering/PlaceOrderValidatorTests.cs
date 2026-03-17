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
            new OrderItemRequest(Guid.NewGuid(), 1)
        ]);

        _validator.TestValidate(command).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ShouldFail_WhenEmailIsEmpty()
    {
        var command = new PlaceOrderCommand("",
        [
            new OrderItemRequest(Guid.NewGuid(), 1)
        ]);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CustomerEmail);
    }

    [Fact]
    public void ShouldFail_WhenEmailIsInvalid()
    {
        var command = new PlaceOrderCommand("not-an-email",
        [
            new OrderItemRequest(Guid.NewGuid(), 1)
        ]);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.CustomerEmail);
    }

    [Fact]
    public void ShouldFail_WhenItemsAreEmpty()
    {
        var command = new PlaceOrderCommand("john@example.com", []);

        _validator.TestValidate(command).ShouldHaveValidationErrorFor(x => x.Items);
    }

    [Fact]
    public void ShouldFail_WhenItemProductIdIsEmpty()
    {
        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemRequest(Guid.Empty, 1)
        ]);

        var result = _validator.TestValidate(command);
        result.ShouldHaveAnyValidationError();
    }

    [Fact]
    public void ShouldFail_WhenItemQuantityIsZero()
    {
        var command = new PlaceOrderCommand("john@example.com",
        [
            new OrderItemRequest(Guid.NewGuid(), 0)
        ]);

        var result = _validator.TestValidate(command);
        result.ShouldHaveAnyValidationError();
    }
}
