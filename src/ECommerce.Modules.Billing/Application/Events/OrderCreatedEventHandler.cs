using ECommerce.Modules.Billing.Domain;
using ECommerce.Shared.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Modules.Billing.Application.Events;

/// <summary>
/// SRP: Handles payment creation when an order is created.
/// </summary>
public sealed class ProcessPaymentOnOrderCreated(
    IPaymentRepository paymentRepository,
    IBillingUnitOfWork unitOfWork,
    ILogger<ProcessPaymentOnOrderCreated> logger) : INotificationHandler<OrderCreatedIntegrationEvent>
{
    public async Task Handle(OrderCreatedIntegrationEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}",
            notification.OrderId, notification.TotalAmount);

        var payment = Payment.Create(notification.OrderId, notification.TotalAmount);
        payment.MarkAsCompleted();

        paymentRepository.Add(payment);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Payment {PaymentId} completed for Order {OrderId}",
            payment.Id, notification.OrderId);
    }
}

/// <summary>
/// SRP: Handles invoice generation when an order is created.
/// </summary>
public sealed class GenerateInvoiceOnOrderCreated(
    IPaymentRepository paymentRepository,
    IInvoiceRepository invoiceRepository,
    IBillingUnitOfWork unitOfWork,
    ILogger<GenerateInvoiceOnOrderCreated> logger) : INotificationHandler<OrderCreatedIntegrationEvent>
{
    public async Task Handle(OrderCreatedIntegrationEvent notification, CancellationToken ct)
    {
        // Find the payment for this order (created by ProcessPaymentOnOrderCreated)
        var payment = (await paymentRepository.QueryByOrderId(notification.OrderId)
            .ToListAsync(ct)).FirstOrDefault();

        var paymentId = payment?.Id ?? Guid.Empty;

        var invoice = Invoice.Create(
            notification.OrderId,
            paymentId,
            notification.CustomerEmail,
            notification.TotalAmount);

        invoiceRepository.Add(invoice);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Invoice {InvoiceNumber} issued for Order {OrderId}",
            invoice.InvoiceNumber, notification.OrderId);
    }
}
