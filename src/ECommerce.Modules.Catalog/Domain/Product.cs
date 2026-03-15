using ECommerce.Shared.Domain;

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

    public static Product Create(string name, string sku, decimal price, int stockQuantity, Guid categoryId) =>
        new()
        {
            Name = name,
            Sku = sku,
            Price = price,
            StockQuantity = stockQuantity,
            CategoryId = categoryId
        };

    public void Update(string name, decimal price)
    {
        Name = name;
        Price = price;
    }

    public bool HasStock(int quantity) => StockQuantity >= quantity;

    public void DecreaseStock(int quantity)
    {
        if (!HasStock(quantity))
            throw new InvalidOperationException($"Insufficient stock for product '{Name}'. Available: {StockQuantity}, Requested: {quantity}");

        StockQuantity -= quantity;
    }

    public void IncreaseStock(int quantity) => StockQuantity += quantity;
}
