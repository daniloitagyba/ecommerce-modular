using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Catalog.Infrastructure;

public sealed class ProductRepository(CatalogDbContext context) : Repository<Product>(context), IProductRepository
{
    public async Task<Product?> GetByIdWithCategoryAsync(Guid id, CancellationToken ct = default) =>
        await DbSet.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id, ct);

    public IQueryable<Product> QueryWithCategory() => DbSet.Include(p => p.Category);
}

public sealed class CategoryRepository(CatalogDbContext context) : Repository<Category>(context), ICategoryRepository;
