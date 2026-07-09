namespace TTRPGHub.Features.Macros.Shared;

public sealed record MacroDto(
    Guid Id, string Name, string? ImageUrl, string Type, string Command,
    int HotbarSlot, DateTime CreatedAt, DateTime UpdatedAt);
