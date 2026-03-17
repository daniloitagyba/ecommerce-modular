using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Ordering.Domain;

public sealed class Order : Entity
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
    {
        [OrderStatus.Pending] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Paid, OrderStatus.Cancelled],
        [OrderStatus.Paid] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [],
        [OrderStatus.Cancelled] = [],
    };

    public string CustomerEmail { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public decimal TotalAmount { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Result<Order> Create(string customerEmail, IEnumerable<OrderItem> items)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
            return Result<Order>.Failure(OrderErrors.EmptyEmail);

        var itemList = items.ToList();
        if (itemList.Count == 0)
            return Result<Order>.Failure(OrderErrors.EmptyItems);

        var order = new Order { CustomerEmail = customerEmail };
        order._items.AddRange(itemList);
        order.TotalAmount = order._items.Sum(l => l.Total);
        order.AddDomainEvent(new OrderCreatedIntegrationEvent(order.Id, order.CustomerEmail, order.TotalAmount));

        return Result<Order>.Success(order);
    }

    public Result Confirm() => TransitionTo(OrderStatus.Confirmed);
    public Result MarkAsPaid() => TransitionTo(OrderStatus.Paid);
    public Result MarkAsShipped() => TransitionTo(OrderStatus.Shipped);
    public Result Cancel() => TransitionTo(OrderStatus.Cancelled);

    private Result TransitionTo(OrderStatus target)
    {
        if (!ValidTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(target))
            return Result.Failure(OrderErrors.InvalidTransition(Status, target));

        Status = target;
        return Result.Success();
    }
}

public static class OrderErrors
{
    public static readonly Error EmptyEmail = new("Order.EmptyEmail", "Customer email is required.");
    public static readonly Error EmptyItems = new("Order.EmptyItems", "Order must have at least one item.");
    public static readonly Error NotFound = new("Order.NotFound", "Order was not found.");

    public static Error InvalidTransition(OrderStatus from, OrderStatus to) =>
        new("Order.InvalidTransition", $"Cannot transition from '{from}' to '{to}'.");
}
