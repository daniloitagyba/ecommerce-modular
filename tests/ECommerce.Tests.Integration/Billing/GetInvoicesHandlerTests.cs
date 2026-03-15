using ECommerce.Modules.Billing.Application.Events;
using ECommerce.Modules.Billing.Application.Queries;
using ECommerce.Shared.Domain;
using ECommerce.Tests.Integration.Fixtures;
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

        // Create payment first
        var paymentLogger = Substitute.For<ILogger<ProcessPaymentOnOrderCreated>>();
        var paymentHandler = new ProcessPaymentOnOrderCreated(paymentRepo, db, paymentLogger);
        await paymentHandler.Handle(new OrderCreatedIntegrationEvent(orderId, "john@example.com", 1500m), CancellationToken.None);

        // Then create invoice
        var invoiceLogger = Substitute.For<ILogger<GenerateInvoiceOnOrderCreated>>();
        var invoiceHandler = new GenerateInvoiceOnOrderCreated(paymentRepo, invoiceRepo, db, invoiceLogger);
        await invoiceHandler.Handle(new OrderCreatedIntegrationEvent(orderId, "john@example.com", 1500m), CancellationToken.None);

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
