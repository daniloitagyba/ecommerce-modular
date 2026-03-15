using ECommerce.Modules.Billing.Application.Events;
using ECommerce.Shared.Domain;
using ECommerce.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ECommerce.Tests.Integration.Billing;

public class OrderCreatedEventHandlerTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    [Fact]
    public async Task Handle_ShouldCreatePaymentAndInvoice()
    {
        await using var db = _factory.CreateBillingContext();
        var paymentRepo = _factory.CreatePaymentRepository(db);
        var invoiceRepo = _factory.CreateInvoiceRepository(db);

        var paymentLogger = Substitute.For<ILogger<ProcessPaymentOnOrderCreated>>();
        var invoiceLogger = Substitute.For<ILogger<GenerateInvoiceOnOrderCreated>>();

        var evt = new OrderCreatedIntegrationEvent(Guid.NewGuid(), "john@example.com", 1999.98m);

        // Process payment first
        var paymentHandler = new ProcessPaymentOnOrderCreated(paymentRepo, db, paymentLogger);
        await paymentHandler.Handle(evt, CancellationToken.None);

        // Then generate invoice
        var invoiceHandler = new GenerateInvoiceOnOrderCreated(paymentRepo, invoiceRepo, db, invoiceLogger);
        await invoiceHandler.Handle(evt, CancellationToken.None);

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == evt.OrderId);
        payment.Should().NotBeNull();
        payment!.Amount.Should().Be(1999.98m);
        payment.Status.Should().Be(Modules.Billing.Domain.PaymentStatus.Completed);
        payment.CompletedAt.Should().NotBeNull();

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.OrderId == evt.OrderId);
        invoice.Should().NotBeNull();
        invoice!.CustomerEmail.Should().Be("john@example.com");
        invoice.Amount.Should().Be(1999.98m);
        invoice.InvoiceNumber.Should().StartWith("INV-");
        invoice.PaymentId.Should().Be(payment.Id);
    }

    public void Dispose() => _factory.Dispose();
}
