using System.Text.RegularExpressions;

namespace ECommerce.Shared.Domain.ValueObjects;

public sealed partial record Sku
{
    public string Value { get; }

    private Sku(string value) => Value = value;

    public static Result<Sku> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Sku>.Failure(SkuErrors.Empty);

        if (value.Length > 50)
            return Result<Sku>.Failure(SkuErrors.TooLong);

        if (!SkuRegex().IsMatch(value))
            return Result<Sku>.Failure(SkuErrors.InvalidFormat);

        return Result<Sku>.Success(new Sku(value.ToUpperInvariant()));
    }

    public static implicit operator string(Sku sku) => sku.Value;

    public override string ToString() => Value;

    [GeneratedRegex(@"^[A-Za-z0-9\-]+$", RegexOptions.Compiled)]
    private static partial Regex SkuRegex();
}

public static class SkuErrors
{
    public static readonly Error Empty = new("Sku.Empty", "SKU cannot be empty.");
    public static readonly Error TooLong = new("Sku.TooLong", "SKU must be 50 characters or less.");
    public static readonly Error InvalidFormat = new("Sku.InvalidFormat", "SKU must contain only letters, numbers, and hyphens.");
}
