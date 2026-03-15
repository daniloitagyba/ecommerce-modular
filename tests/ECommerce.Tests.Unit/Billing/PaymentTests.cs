using ECommerce.Modules.Billing.Domain;

namespace ECommerce.Tests.Unit.Billing;

public class PaymentTests
{
    [Fact]
    public void Create_ShouldInitializeWithPendingStatus()
    {
        var orderId = Guid.NewGuid();

        var payment = Payment.Create(orderId, 1999.98m);

        payment.Id.Should().NotBeEmpty();
        payment.OrderId.Should().Be(orderId);
        payment.Amount.Should().Be(1999.98m);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsCompleted_ShouldUpdateStatusAndTimestamp()
    {
        var payment = Payment.Create(Guid.NewGuid(), 100m);

        payment.MarkAsCompleted();

        payment.Status.Should().Be(PaymentStatus.Completed);
        payment.CompletedAt.Should().NotBeNull();
        payment.CompletedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatus()
    {
        var payment = Payment.Create(Guid.NewGuid(), 100m);

        payment.MarkAsFailed();

        payment.Status.Should().Be(PaymentStatus.Failed);
        payment.CompletedAt.Should().BeNull();
    }
}
