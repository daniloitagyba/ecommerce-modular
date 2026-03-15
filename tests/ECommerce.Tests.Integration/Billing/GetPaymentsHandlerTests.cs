using ECommerce.Modules.Billing.Application.Events;
using ECommerce.Modules.Billing.Application.Queries;
using ECommerce.Shared.Domain;
using ECommerce.Tests.Integration.Fixtures;
using MassTransit;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ECommerce.Tests.Integration.Billing;

public class GetPaymentsHandlerTests : IDisposable
{
    private readonly DbContextFactory _factory = new();

    [Fact]
    public async Task Handle_ShouldReturnPaymentsForOrder()
    {
        await using var db = _factory.CreateBillingContext();
        var paymentRepo = _factory.CreatePaymentRepository(db);
        var invoiceRepo = _factory.CreateInvoiceRepository(db);
        var orderId = Guid.NewGuid();

        // Create payment via consumer
        var logger = Substitute.For<ILogger<OrderCreatedConsumer>>();
        var consumer = new OrderCreatedConsumer(paymentRepo, invoiceRepo, db, logger);
        var consumeContext = Substitute.For<ConsumeContext<OrderCreatedIntegrationEvent>>();
        consumeContext.Message.Returns(new OrderCreatedIntegrationEvent(orderId, "a@b.com", 500m));
        consumeContext.CancellationToken.Returns(CancellationToken.None);
        await consumer.Consume(consumeContext);

        var handler = new GetPaymentsByOrderHandler(paymentRepo);

        var result = await handler.Handle(new GetPaymentsByOrderQuery(orderId), CancellationToken.None);

        result.Should().ContainSingle()
            .Which.Should().Match<PaymentDto>(p =>
                p.OrderId == orderId && p.Amount == 500m && p.Status == "Completed");
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenNoPayments()
    {
        await using var db = _factory.CreateBillingContext();
        var paymentRepo = _factory.CreatePaymentRepository(db);
        var handler = new GetPaymentsByOrderHandler(paymentRepo);

        var result = await handler.Handle(new GetPaymentsByOrderQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeEmpty();
    }

    public void Dispose() => _factory.Dispose();
}
