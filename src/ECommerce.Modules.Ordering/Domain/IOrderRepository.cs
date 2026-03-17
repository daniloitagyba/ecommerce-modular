using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Ordering.Domain;

public interface IOrderingUnitOfWork : IUnitOfWork;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);
    IQueryable<Order> QueryWithItems();
}
