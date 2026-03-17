using ECommerce.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Shared.Infrastructure;

public class Repository<T>(DbContext context) : IRepository<T> where T : Entity
{
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.FindAsync([id], ct);

    public async Task<List<T>> GetAllAsync(int limit = 100, CancellationToken ct = default) =>
        await DbSet.Take(limit).ToListAsync(ct);

    public IQueryable<T> Query() => DbSet.AsQueryable();

    public void Add(T entity) => DbSet.Add(entity);

    public void Remove(T entity) => DbSet.Remove(entity);
}
