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

    public static Order Create(string customerEmail, IEnumerable<OrderLine> lines)
    {
        var order = new Order { CustomerEmail = customerEmail };
        order._lines.AddRange(lines);
        order.AddDomainEvent(new OrderCreatedIntegrationEvent(order.Id, order.CustomerEmail, order.TotalAmount));
        return order;
    }

    public void MarkAsPaid() => Status = OrderStatus.Paid;
    public void MarkAsShipped() => Status = OrderStatus.Shipped;
    public void Cancel() => Status = OrderStatus.Cancelled;
}
