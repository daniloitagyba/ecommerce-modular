using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Ordering.Domain;

public interface IOrderingUnitOfWork : IUnitOfWork;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default);
    IQueryable<Order> QueryWithLines();
}
