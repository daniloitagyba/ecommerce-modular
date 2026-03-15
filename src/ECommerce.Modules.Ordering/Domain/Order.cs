using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Ordering.Domain;

public sealed class Order : Entity
{
    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public decimal TotalAmount => _lines.Sum(l => l.Total);

    private readonly List<OrderLine> _lines = [];
    public IReadOnlyList<OrderLine> Lines => _lines.AsReadOnly();

    private Order() { }

    public static Result<Order> Create(string customerEmail, IEnumerable<OrderLine> lines)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            return Result<Order>.Failure(OrderErrors.EmptyEmail);

        var lineList = lines.ToList();
        if (lineList.Count == 0)
            return Result<Order>.Failure(OrderErrors.EmptyLines);

        var order = new Order { CustomerEmail = customerEmail };
        order._lines.AddRange(lineList);
        order.AddDomainEvent(new OrderCreatedIntegrationEvent(order.Id, order.CustomerEmail, order.TotalAmount));

        return Result<Order>.Success(order);
    }

    public void MarkAsPaid() => Status = OrderStatus.Paid;
    public void MarkAsShipped() => Status = OrderStatus.Shipped;
    public void Cancel() => Status = OrderStatus.Cancelled;
}

public static class OrderErrors
{
    public static readonly Error EmptyEmail = new("Order.EmptyEmail", "Customer email is required.");
    public static readonly Error EmptyLines = new("Order.EmptyLines", "Order must have at least one item.");
    public static readonly Error NotFound = new("Order.NotFound", "Order was not found.");
}
