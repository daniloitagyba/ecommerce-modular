using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Billing.Domain;

public sealed class Invoice : Entity
{
    public Guid OrderId { get; private set; }
    public Guid PaymentId { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public DateTime IssuedAt { get; private set; } = DateTime.UtcNow;

    private Invoice() { }

    public static Invoice Create(Guid orderId, Guid paymentId, string customerEmail, decimal amount) =>
        new()
        {
            OrderId = orderId,
            PaymentId = paymentId,
            CustomerEmail = customerEmail,
            Amount = amount,
            InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}"
        };
}
