using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Ordering.Domain;

public sealed class OrderItem : Entity
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public decimal Total => UnitPrice * Quantity;

    private OrderItem() { }

    public static OrderItem Create(Guid productId, string productName, decimal unitPrice, int quantity) =>
        new()
        {
            ProductId = productId,
            ProductName = productName,
            UnitPrice = unitPrice,
            Quantity = quantity
        };
}
