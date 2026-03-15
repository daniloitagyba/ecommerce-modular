using ECommerce.Shared.Domain;

namespace ECommerce.Modules.Catalog.Domain;

public sealed class Category : Entity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    private readonly List<Product> _products = [];
    public IReadOnlyList<Product> Products => _products.AsReadOnly();

    private Category() { }

    public static Category Create(string name, string description) =>
        new() { Name = name, Description = description };

    public void Update(string name, string description)
    {
        Name = name;
        Description = description;
    }
}
