using ECommerce.Modules.Billing.Domain;
using ECommerce.Shared.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Modules.Billing.Application.Events;

/// <summary>
/// MassTransit consumer: processes payment and generates invoice when an order is created.
/// Replaces the former synchronous MediatR notification handlers with async outbox-driven processing.
/// </summary>
public sealed class OrderCreatedConsumer(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository,
    IBillingUnitOfWork unitOfWork,
    ILogger<OrderCreatedConsumer> logger) : IConsumer<OrderCreatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var notification = context.Message;

        // 1. Process payment
        logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}",
            notification.OrderId, notification.TotalAmount);

        var payment = Payment.Create(notification.OrderId, notification.TotalAmount);
        payment.MarkAsCompleted();

        paymentRepository.Add(payment);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Payment {PaymentId} completed for Order {OrderId}",
            payment.Id, notification.OrderId);

        // 2. Generate invoice
        var invoice = Invoice.Create(
            notification.OrderId,
            payment.Id,
            notification.CustomerEmail,
            notification.TotalAmount);

        invoiceRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Invoice {InvoiceNumber} issued for Order {OrderId}",
            invoice.InvoiceNumber, notification.OrderId);
    }
}
