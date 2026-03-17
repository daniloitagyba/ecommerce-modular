using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Domain;

namespace ECommerce.Tests.Unit.Ordering;

public class OrderTests
{
    [Fact]
    public void Create_ShouldSucceedWithValidInput()
    {
        var items = new[]
        {
            OrderItem.Create(Guid.NewGuid(), "Laptop", 1000m, 1),
            OrderItem.Create(Guid.NewGuid(), "Mouse", 50m, 2)
        };

        var result = Order.Create("john@example.com", items);

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerEmail.Should().Be("john@example.com");
        result.Value.Status.Should().Be(OrderStatus.Pending);
        result.Value.Items.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ShouldFail_WhenEmailIsEmpty()
    {
        var items = new[] { OrderItem.Create(Guid.NewGuid(), "P", 10m, 1) };

        var result = Order.Create("", items);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.EmptyEmail");
    }

    [Fact]
    public void Create_ShouldFail_WhenItemsAreEmpty()
    {
        var result = Order.Create("john@example.com", []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.EmptyItems");
    }

    [Fact]
    public void Create_ShouldCalculateTotalAmount()
    {
        var items = new[]
        {
            OrderItem.Create(Guid.NewGuid(), "Laptop", 1000m, 2),
            OrderItem.Create(Guid.NewGuid(), "Mouse", 50m, 3)
        };

        var order = Order.Create("john@example.com", items).Value;

        order.TotalAmount.Should().Be(2150m);
    }

    [Fact]
    public void Create_ShouldRaiseOrderCreatedIntegrationEvent()
    {
        var items = new[] { OrderItem.Create(Guid.NewGuid(), "Laptop", 1000m, 1) };

        var order = Order.Create("john@example.com", items).Value;

        order.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<OrderCreatedIntegrationEvent>()
            .Which.Should().BeEquivalentTo(new
            {
                OrderId = order.Id,
                CustomerEmail = "john@example.com",
                TotalAmount = 1000m
            });
    }

    [Fact]
    public void Confirm_ShouldChangeStatus_FromPending()
    {
        var order = CreateOrder();
        var result = order.Confirm();
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public void MarkAsPaid_ShouldChangeStatus_FromConfirmed()
    {
        var order = CreateOrder();
        order.Confirm();
        var result = order.MarkAsPaid();
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public void MarkAsShipped_ShouldChangeStatus_FromPaid()
    {
        var order = CreateOrder();
        order.Confirm();
        order.MarkAsPaid();
        var result = order.MarkAsShipped();
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Cancel_ShouldChangeStatus_FromPending()
    {
        var order = CreateOrder();
        var result = order.Cancel();
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }

    [Fact]
    public void MarkAsPaid_ShouldFail_FromPending()
    {
        var order = CreateOrder();
        var result = order.MarkAsPaid();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.InvalidTransition");
    }

    [Fact]
    public void MarkAsShipped_ShouldFail_FromPending()
    {
        var order = CreateOrder();
        var result = order.MarkAsShipped();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.InvalidTransition");
    }

    [Fact]
    public void Confirm_ShouldFail_FromCancelled()
    {
        var order = CreateOrder();
        order.Cancel();
        var result = order.Confirm();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.InvalidTransition");
    }

    [Fact]
    public void MarkAsShipped_ShouldFail_FromConfirmed()
    {
        var order = CreateOrder();
        order.Confirm();
        var result = order.MarkAsShipped();
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.InvalidTransition");
    }

    private static Order CreateOrder() =>
        Order.Create("a@b.com", [OrderItem.Create(Guid.NewGuid(), "P", 10m, 1)]).Value;
}
