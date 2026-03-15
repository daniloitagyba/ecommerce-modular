using ECommerce.Modules.Ordering.Domain;

namespace ECommerce.Tests.Unit.Ordering;

public class OrderLineTests
{
    [Fact]
    public void Create_ShouldInitializeAllProperties()
    {
        var productId = Guid.NewGuid();

        var line = OrderLine.Create(productId, "Laptop", 999.99m, 2);

        line.Id.Should().NotBeEmpty();
        line.ProductId.Should().Be(productId);
        line.ProductName.Should().Be("Laptop");
        line.UnitPrice.Should().Be(999.99m);
        line.Quantity.Should().Be(2);
    }

    [Fact]
    public void Total_ShouldCalculateCorrectly()
    {
        var line = OrderLine.Create(Guid.NewGuid(), "Laptop", 999.99m, 3);

        line.Total.Should().Be(2999.97m);
    }
}
