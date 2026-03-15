using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Domain;

namespace ECommerce.Tests.Unit.Ordering;

public class OrderTests
{
    [Fact]
    public void Create_ShouldSucceedWithValidInput()
    {
        var lines = new[]
        {
            OrderItem.Create(Guid.NewGuid(), "Laptop", 1000m, 1),
            OrderItem.Create(Guid.NewGuid(), "Mouse", 50m, 2)
        };

        var result = Order.Create("john@example.com", lines);

        result.IsSuccess.Should().BeTrue();
        result.Value!.CustomerEmail.Should().Be("john@example.com");
        result.Value.Status.Should().Be(OrderStatus.Pending);
        result.Value.Lines.Should().HaveCount(2);
    }

    [Fact]
    public void Create_ShouldFail_WhenEmailIsEmpty()
    {
        var lines = new[] { OrderItem.Create(Guid.NewGuid(), "P", 10m, 1) };

        var result = Order.Create("", lines);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.EmptyEmail");
    }

    [Fact]
    public void Create_ShouldFail_WhenLinesAreEmpty()
    {
        var result = Order.Create("john@example.com", []);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.EmptyLines");
    }

    [Fact]
    public void Create_ShouldCalculateTotalAmount()
    {
        var lines = new[]
        {
            OrderItem.Create(Guid.NewGuid(), "Laptop", 1000m, 2),
            OrderItem.Create(Guid.NewGuid(), "Mouse", 50m, 3)
        };

        var order = Order.Create("john@example.com", lines).Value!;

        order.TotalAmount.Should().Be(2150m);
    }

    [Fact]
    public void Create_ShouldRaiseOrderCreatedIntegrationEvent()
    {
        var lines = new[] { OrderItem.Create(Guid.NewGuid(), "Laptop", 1000m, 1) };

        var order = Order.Create("john@example.com", lines).Value!;

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
    public void MarkAsPaid_ShouldChangeStatus()
    {
        var order = Order.Create("a@b.com", [OrderItem.Create(Guid.NewGuid(), "P", 10m, 1)]).Value!;
        order.MarkAsPaid();
        order.Status.Should().Be(OrderStatus.Paid);
    }

    [Fact]
    public void MarkAsShipped_ShouldChangeStatus()
    {
        var order = Order.Create("a@b.com", [OrderItem.Create(Guid.NewGuid(), "P", 10m, 1)]).Value!;
        order.MarkAsShipped();
        order.Status.Should().Be(OrderStatus.Shipped);
    }

    [Fact]
    public void Cancel_ShouldChangeStatus()
    {
        var order = Order.Create("a@b.com", [OrderItem.Create(Guid.NewGuid(), "P", 10m, 1)]).Value!;
        order.Cancel();
        order.Status.Should().Be(OrderStatus.Cancelled);
    }
}
