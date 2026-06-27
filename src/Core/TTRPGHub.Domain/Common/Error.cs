namespace TTRPGHub.Domain.Common;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string resource) =>
        new($"{resource}.NotFound", $"{resource} не найден.");

    public static Error Conflict(string resource) =>
        new($"{resource}.Conflict", $"{resource} уже существует.");

    public static Error Unauthorized() =>
        new("Auth.Unauthorized", "Нет доступа.");

    public static Error Validation(string field, string message) =>
        new($"Validation.{field}", message);
}
