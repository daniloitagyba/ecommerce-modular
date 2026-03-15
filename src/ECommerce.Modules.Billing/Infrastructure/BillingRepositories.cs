using ECommerce.Modules.Billing.Domain;
using ECommerce.Shared.Infrastructure;

namespace ECommerce.Modules.Billing.Infrastructure;

public sealed class PaymentRepository(BillingDbContext context) : Repository<Payment>(context), IPaymentRepository
{
    public IQueryable<Payment> QueryByOrderId(Guid orderId) =>
        DbSet.Where(p => p.OrderId == orderId);
}

public sealed class InvoiceRepository(BillingDbContext context) : Repository<Invoice>(context), IInvoiceRepository
{
    public IQueryable<Invoice> QueryByOrderId(Guid orderId) =>
        DbSet.Where(i => i.OrderId == orderId);
}
