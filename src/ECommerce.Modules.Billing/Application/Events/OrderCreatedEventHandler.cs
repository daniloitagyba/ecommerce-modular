using ECommerce.Modules.Billing.Domain;
using ECommerce.Modules.Billing.Infrastructure;
using ECommerce.Shared.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ECommerce.Modules.Billing.Application.Events;

public sealed class OrderCreatedEventHandler(
    BillingDbContext db,
    ILogger<OrderCreatedEventHandler> logger) : INotificationHandler<OrderCreatedIntegrationEvent>
{
    public async Task Handle(OrderCreatedIntegrationEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Processing payment for Order {OrderId}, Amount: {Amount}",
            notification.OrderId, notification.TotalAmount);

        var payment = Payment.Create(notification.OrderId, notification.TotalAmount);

        // Simulate payment processing — in production, call a payment gateway here
        payment.MarkAsCompleted();

        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        // Generate invoice after successful payment
        var invoice = Invoice.Create(
            notification.OrderId,
            payment.Id,
            notification.CustomerEmail,
            notification.TotalAmount);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Payment completed and Invoice {InvoiceNumber} issued for Order {OrderId}",
            invoice.InvoiceNumber, notification.OrderId);
    }
}
