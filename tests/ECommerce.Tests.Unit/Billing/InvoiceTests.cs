using ECommerce.Modules.Billing.Domain;

namespace ECommerce.Tests.Unit.Billing;

public class InvoiceTests
{
    [Fact]
    public void Create_ShouldInitializeAllProperties()
    {
        var orderId = Guid.NewGuid();
        var paymentId = Guid.NewGuid();

        var invoice = Invoice.Create(orderId, paymentId, "john@example.com", 1999.98m);

        invoice.Id.Should().NotBeEmpty();
        invoice.OrderId.Should().Be(orderId);
        invoice.PaymentId.Should().Be(paymentId);
        invoice.CustomerEmail.Should().Be("john@example.com");
        invoice.Amount.Should().Be(1999.98m);
        invoice.InvoiceNumber.Should().StartWith("INV-");
        invoice.IssuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueInvoiceNumbers()
    {
        var invoice1 = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "a@b.com", 100m);
        var invoice2 = Invoice.Create(Guid.NewGuid(), Guid.NewGuid(), "a@b.com", 200m);

        invoice1.InvoiceNumber.Should().NotBe(invoice2.InvoiceNumber);
    }
}
