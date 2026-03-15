using ECommerce.Modules.Ordering.Domain;
using ECommerce.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Ordering.Infrastructure;

public sealed class OrderRepository(OrderingDbContext context) : Repository<Order>(context), IOrderRepository
{
    public async Task<Order?> GetByIdWithLinesAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id, ct);

    public IQueryable<Order> QueryWithLines() => DbSet.Include(o => o.Lines);
}
