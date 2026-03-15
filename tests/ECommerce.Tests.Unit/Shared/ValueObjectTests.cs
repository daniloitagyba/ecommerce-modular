using ECommerce.Shared.Domain.ValueObjects;

namespace ECommerce.Tests.Unit.Shared;

public class EmailTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidEmail()
    {
        var result = Email.Create("test@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_ShouldNormalize_ToLowerCase()
    {
        var result = Email.Create("Test@EXAMPLE.com");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_ShouldFail_WhenEmpty()
    {
        var result = Email.Create("");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.Empty");
    }

    [Fact]
    public void Create_ShouldFail_WhenTooLong()
    {
        var longEmail = new string('a', 252) + "@b.com";
        var result = Email.Create(longEmail);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.TooLong");
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidFormat()
    {
        var result = Email.Create("not-an-email");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Email.InvalidFormat");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnValue()
    {
        var email = Email.Create("test@example.com").Value!;
        string value = email;
        value.Should().Be("test@example.com");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var email = Email.Create("test@example.com").Value!;
        email.ToString().Should().Be("test@example.com");
    }
}

public class MoneyTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidAmount()
    {
        var result = Money.Create(100m);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Amount.Should().Be(100m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_ShouldFail_WhenNegative()
    {
        var result = Money.Create(-1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.NegativeAmount");
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidCurrency()
    {
        var result = Money.Create(10m, "US");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.InvalidCurrency");
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        var money = Money.Zero();
        money.Amount.Should().Be(0m);
        money.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_ShouldSumAmounts()
    {
        var a = Money.Create(100m).Value!;
        var b = Money.Create(50m).Value!;

        var sum = a.Add(b);

        sum.Amount.Should().Be(150m);
    }

    [Fact]
    public void Add_ShouldThrow_WhenDifferentCurrencies()
    {
        var usd = Money.Create(100m, "USD").Value!;
        var eur = Money.Create(50m, "EUR").Value!;

        var act = () => usd.Add(eur);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Multiply_ShouldMultiplyAmount()
    {
        var money = Money.Create(25m).Value!;

        var result = money.Multiply(4);

        result.Amount.Should().Be(100m);
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnAmount()
    {
        var money = Money.Create(42m).Value!;
        decimal value = money;
        value.Should().Be(42m);
    }
}

public class SkuTests
{
    [Fact]
    public void Create_ShouldSucceed_WithValidSku()
    {
        var result = Sku.Create("LAP-001");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("LAP-001");
    }

    [Fact]
    public void Create_ShouldNormalize_ToUpperCase()
    {
        var result = Sku.Create("lap-001");

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("LAP-001");
    }

    [Fact]
    public void Create_ShouldFail_WhenEmpty()
    {
        var result = Sku.Create("");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sku.Empty");
    }

    [Fact]
    public void Create_ShouldFail_WhenTooLong()
    {
        var result = Sku.Create(new string('A', 51));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sku.TooLong");
    }

    [Fact]
    public void Create_ShouldFail_WhenInvalidFormat()
    {
        var result = Sku.Create("INVALID SKU!!");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Sku.InvalidFormat");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnValue()
    {
        var sku = Sku.Create("SKU-1").Value!;
        string value = sku;
        value.Should().Be("SKU-1");
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        var sku = Sku.Create("SKU-1").Value!;
        sku.ToString().Should().Be("SKU-1");
    }
}
