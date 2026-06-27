using TTRPGHub.Domain.Common;

namespace TTRPGHub.Domain.ValueObjects;

public sealed class Email : IEquatable<Email>
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Result<Email> Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation(nameof(Email), "Email не может быть пустым.");

        if (!value.Contains('@') || value.Length > 256)
            return Error.Validation(nameof(Email), "Некорректный формат email.");

        return new Email(value.Trim().ToLowerInvariant());
    }

    public bool Equals(Email? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is Email email && Equals(email);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value;
}
