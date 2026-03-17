using ECommerce.Modules.Billing.Domain;
using ECommerce.Shared.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Modules.Billing.Application.Events;

/// <summary>
/// MassTransit consumer: processes payment and generates invoice when an order is created.
/// Idempotent — skips if a payment for the order already exists.
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

        var existingPayment = await paymentRepository.QueryByOrderId(notification.OrderId)
            .FirstOrDefaultAsync(context.CancellationToken);

        if (existingPayment is not null)
        {
            logger.LogInformation("Payment already exists for Order {OrderId}, skipping", notification.OrderId);
            return;
        }

        logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}",
            notification.OrderId, notification.TotalAmount);

        var payment = Payment.Create(notification.OrderId, notification.TotalAmount);
        payment.MarkAsCompleted();
        paymentRepository.Add(payment);

        var invoice = Invoice.Create(
            notification.OrderId,
            payment.Id,
            notification.CustomerEmail,
            notification.TotalAmount);
        invoiceRepository.Add(invoice);

        await unitOfWork.SaveChangesAsync(context.CancellationToken);

        logger.LogInformation("Payment {PaymentId} and Invoice {InvoiceNumber} created for Order {OrderId}",
            payment.Id, invoice.InvoiceNumber, notification.OrderId);
    }
}
