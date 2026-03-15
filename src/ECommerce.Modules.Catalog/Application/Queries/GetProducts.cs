using ECommerce.Modules.Catalog.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Catalog.Application.Queries;

public sealed record GetProductsQuery : IRequest<List<ProductDto>>;

public sealed record ProductDto(Guid Id, string Name, string Sku, decimal Price, int StockQuantity, string CategoryName);

public sealed class GetProductsHandler(CatalogDbContext db) : IRequestHandler<GetProductsQuery, List<ProductDto>>
{
    public async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct) =>
        await db.Products
            .Include(p => p.Category)
            .Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.Price, p.StockQuantity, p.Category.Name))
            .ToListAsync(ct);
}
