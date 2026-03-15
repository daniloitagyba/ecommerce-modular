using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Billing.Domain;

public interface IBillingUnitOfWork : IUnitOfWork;

public interface IPaymentRepository : IRepository<Payment>
{
    IQueryable<Payment> QueryByOrderId(Guid orderId);
}

public interface IInvoiceRepository : IRepository<Invoice>
{
    IQueryable<Invoice> QueryByOrderId(Guid orderId);
}
