using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Domain;
using MediatR;

namespace ECommerce.Modules.Catalog.Application.Queries;

public sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductDto>>;

public sealed class GetProductByIdHandler(IProductRepository repository)
    : IRequestHandler<GetProductByIdQuery, Result<ProductDto>>
{
    public async Task<Result<ProductDto>> Handle(GetProductByIdQuery request, CancellationToken ct)
    {
        var product = await repository.GetByIdWithCategoryAsync(request.Id, ct);
        if (product is null)
            return Result<ProductDto>.Failure(ProductErrors.NotFound);

        return Result<ProductDto>.Success(
            new ProductDto(product.Id, product.Name, product.Sku, product.Price, product.StockQuantity, product.Category.Name));
    }
}
