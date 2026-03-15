using ECommerce.Shared.Domain;
using VO = ECommerce.Shared.Domain.ValueObjects;

namespace ECommerce.Modules.Catalog.Domain;

public sealed class Product : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }
    public Guid CategoryId { get; private set; }
    public Category Category { get; private set; } = null!;

    private Product() { }

    public static Result<Product> Create(string name, string sku, decimal price, int stockQuantity, Guid categoryId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result<Product>.Failure(ProductErrors.EmptyName);

        var skuResult = VO.Sku.Create(sku);
        if (skuResult.IsFailure)
            return Result<Product>.Failure(skuResult.Error);

        var moneyResult = VO.Money.Create(price);
        if (moneyResult.IsFailure)
            return Result<Product>.Failure(moneyResult.Error);

        if (stockQuantity < 0)
            return Result<Product>.Failure(ProductErrors.NegativeStock);

        if (categoryId == Guid.Empty)
            return Result<Product>.Failure(ProductErrors.EmptyCategory);

        return Result<Product>.Success(new Product
        {
            Name = name,
            Sku = skuResult.Value!.Value,
            Price = moneyResult.Value!.Amount,
            StockQuantity = stockQuantity,
            CategoryId = categoryId
        });
    }

    public void Update(string name, decimal price)
    {
        Name = name;
        Price = price;
    }

    public bool HasStock(int quantity) => StockQuantity >= quantity;

    public Result DecreaseStock(int quantity)
    {
        if (!HasStock(quantity))
            return Result.Failure(ProductErrors.InsufficientStock(Name, StockQuantity, quantity));

        StockQuantity -= quantity;
        return Result.Success();
    }

    public void IncreaseStock(int quantity) => StockQuantity += quantity;
}

public static class ProductErrors
{
    public static readonly Error EmptyName = new("Product.EmptyName", "Product name cannot be empty.");
    public static readonly Error NegativeStock = new("Product.NegativeStock", "Stock quantity cannot be negative.");
    public static readonly Error EmptyCategory = new("Product.EmptyCategory", "Category must be specified.");
    public static readonly Error NotFound = new("Product.NotFound", "Product was not found.");

    public static Error InsufficientStock(string name, int available, int requested) =>
        new("Product.InsufficientStock", $"Insufficient stock for '{name}'. Available: {available}, Requested: {requested}.");
}
