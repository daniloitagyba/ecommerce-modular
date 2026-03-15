using ECommerce.Modules.Billing.Application.Events;
using ECommerce.Modules.Billing.Application.Queries;
using ECommerce.Shared.Domain;
using ECommerce.Tests.Integration.Fixtures;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ECommerce.Tests.Integration.Billing;

public class GetInvoicesHandlerTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    [Fact]
    public async Task Handle_ShouldReturnInvoicesForOrder()
    {
        await using var db = _factory.CreateBillingContext();
        var paymentRepo = _factory.CreatePaymentRepository(db);
        var invoiceRepo = _factory.CreateInvoiceRepository(db);
        var orderId = Guid.NewGuid();

        // Create payment + invoice via consumer
        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var consumer = new OrderCreatedConsumer(paymentRepo, invoiceRepo, db, logger);
        var consumeContext = Substitute.For<ConsumeContext<OrderCreatedIntegrationEvent>>();
        consumeContext.Message.Returns(new OrderCreatedIntegrationEvent(orderId, "john@example.com", 1500m));
        consumeContext.CancellationToken.Returns(CancellationToken.None);
        await consumer.Consume(consumeContext);

        var handler = new GetInvoicesByOrderHandler(invoiceRepo);

        var result = await handler.Handle(new GetInvoicesByOrderQuery(orderId), CancellationToken.None);

        result.Should().ContainSingle()
            .Which.Should().Match<InvoiceDto>(i =>
                i.OrderId == orderId &&
                i.Amount == 1500m &&
                i.CustomerEmail == "john@example.com" &&
                i.InvoiceNumber.StartsWith("INV-"));
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoInvoices()
    {
        await using var db = _factory.CreateBillingContext();
        var invoiceRepo = _factory.CreateInvoiceRepository(db);
        var handler = new GetInvoicesByOrderHandler(invoiceRepo);

        var result = await handler.Handle(new GetInvoicesByOrderQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    public void Dispose() => _factory.Dispose();
}
