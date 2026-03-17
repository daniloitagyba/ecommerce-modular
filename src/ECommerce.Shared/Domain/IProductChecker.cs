namespace ECommerce.Shared.Domain;

public interface IProductChecker
{
    Task<Result<IReadOnlyList<ValidatedProduct>>> ValidateProductsAsync(
        IReadOnlyList<ProductLineRequest> lines,
        CancellationToken ct = default);
}

public sealed record ProductLineRequest(Guid ProductId, int Quantity);

public sealed record ValidatedProduct(Guid ProductId, string ProductName, decimal UnitPrice, int Quantity);
