using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Catalog.Domain;

public interface ICatalogUnitOfWork : IUnitOfWork;

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetByIdWithCategoryAsync(Guid id, CancellationToken ct = default);
    IQueryable<Product> QueryWithCategory();
}

public interface ICategoryRepository : IRepository<Category>;
