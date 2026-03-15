namespace ECommerce.Shared.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "USD")
    {
        if (amount < 0)
            return Result<Money>.Failure(MoneyErrors.NegativeAmount);

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result<Money>.Failure(MoneyErrors.InvalidCurrency);

        return Result<Money>.Success(new Money(amount, currency.ToUpperInvariant()));
    }

    public static Money Zero(string currency = "USD") => new(0, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {Currency} and {other.Currency}.");

        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(int quantity) => new(Amount * quantity, Currency);

    public static implicit operator decimal(Money money) => money.Amount;
}

public static class MoneyErrors
{
    public static readonly Error NegativeAmount = new("Money.NegativeAmount", "Amount cannot be negative.");
    public static readonly Error InvalidCurrency = new("Money.InvalidCurrency", "Currency must be a 3-letter ISO code.");
}
