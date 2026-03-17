using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Application;
using MediatR;

namespace ECommerce.Modules.Catalog.Application.Queries;

public sealed record GetProductsQuery(PagedRequest Paging) : IRequest<PagedResult<ProductDto>>;

public sealed record ProductDto(Guid Id, string Name, string Sku, decimal Price, int StockQuantity, string CategoryName);

public sealed class GetProductsHandler(IProductRepository repository)
    : IRequestHandler<GetProductsQuery, PagedResult<ProductDto>>
{
    public async Task<PagedResult<ProductDto>> Handle(GetProductsQuery request, CancellationToken ct) =>
        await repository.QueryWithCategory()
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto(p.Id, p.Name, p.Sku, p.Price, p.StockQuantity, p.Category.Name))
            .ToPagedResultAsync(request.Paging, ct);
}
