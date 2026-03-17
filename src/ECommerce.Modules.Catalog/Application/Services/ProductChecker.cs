using ECommerce.Modules.Catalog.Domain;
using ECommerce.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Modules.Catalog.Application.Services;

public sealed class ProductChecker(IProductRepository productRepository, ICatalogUnitOfWork unitOfWork) : IProductChecker
{
    public async Task<Result<IReadOnlyList<ValidatedProduct>>> ValidateProductsAsync(
        IReadOnlyList<ProductLineRequest> lines,
        CancellationToken ct = default)
    {
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();

        var products = await productRepository.Query()
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync(ct);

        if (products.Count != productIds.Count)
        {
            var found = products.Select(p => p.Id).ToHashSet();
            var missing = productIds.First(id => !found.Contains(id));
            return Result<IReadOnlyList<ValidatedProduct>>.Failure(
                new Error("Product.NotFound", $"Product '{missing}' was not found."));
        }

        var productMap = products.ToDictionary(p => p.Id);
        var validated = new List<ValidatedProduct>();

        foreach (var line in lines)
        {
            var product = productMap[line.ProductId];
            var stockResult = product.DecreaseStock(line.Quantity);
            if (stockResult.IsFailure)
                return Result<IReadOnlyList<ValidatedProduct>>.Failure(stockResult.Error);

            validated.Add(new ValidatedProduct(product.Id, product.Name, product.Price, line.Quantity));
        }

        await unitOfWork.SaveChangesAsync(ct);

        return Result<IReadOnlyList<ValidatedProduct>>.Success(validated);
    }
}
