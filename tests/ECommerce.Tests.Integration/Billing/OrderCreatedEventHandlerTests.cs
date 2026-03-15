using ECommerce.Modules.Billing.Application.Events;
using ECommerce.Shared.Domain;
using ECommerce.Tests.Integration.Fixtures;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ECommerce.Tests.Integration.Billing;

public class OrderCreatedConsumerTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    [Fact]
    public async Task Consume_ShouldCreatePaymentAndInvoice()
    {
        await using var db = _factory.CreateBillingContext();
        var paymentRepo = _factory.CreatePaymentRepository(db);
        var invoiceRepo = _factory.CreateInvoiceRepository(db);

        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var consumer = new OrderCreatedConsumer(paymentRepo, invoiceRepo, db, logger);

        var evt = new OrderCreatedIntegrationEvent(Guid.NewGuid(), "john@example.com", 1999.98m);
        var consumeContext = Substitute.For<ConsumeContext<OrderCreatedIntegrationEvent>>();
        consumeContext.Message.Returns(evt);
        consumeContext.CancellationToken.Returns(CancellationToken.None);

        await consumer.Consume(consumeContext);

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
