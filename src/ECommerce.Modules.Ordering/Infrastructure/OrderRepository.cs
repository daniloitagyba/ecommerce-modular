using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Ordering.Infrastructure;

public sealed class OrderRepository(OrderingDbContext context) : Repository<Order>(context), IOrderRepository
{
    public async Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);

    public IQueryable<Order> QueryWithItems() => DbSet.Include(o => o.Items);
}
