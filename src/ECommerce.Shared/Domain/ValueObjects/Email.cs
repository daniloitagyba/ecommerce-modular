using System.Text.RegularExpressions;

namespace ECommerce.Shared.Domain.ValueObjects;

public sealed partial record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Result<Email>.Failure(EmailErrors.Empty);

        if (value.Length > 256)
            return Result<Email>.Failure(EmailErrors.TooLong);

        if (!EmailRegex().IsMatch(value))
            return Result<Email>.Failure(EmailErrors.InvalidFormat);

        return Result<Email>.Success(new Email(value.ToLowerInvariant()));
    }

    public static implicit operator string(Email email) => email.Value;

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled)]
    private static partial Regex EmailRegex();
}

public static class EmailErrors
{
    public static readonly Error Empty = new("Email.Empty", "Email address cannot be empty.");
    public static readonly Error TooLong = new("Email.TooLong", "Email address is too long.");
    public static readonly Error InvalidFormat = new("Email.InvalidFormat", "Email address format is invalid.");
}
