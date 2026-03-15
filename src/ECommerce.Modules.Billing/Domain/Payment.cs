using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Billing.Domain;

public sealed class Payment : Entity
{
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public PaymentStatus Status { get; private set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; private set; }

    private Payment() { }

    public static Payment Create(Guid orderId, decimal amount) =>
        new() { OrderId = orderId, Amount = amount };

    public void MarkAsCompleted()
    {
        Status = PaymentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed() => Status = PaymentStatus.Failed;
}
