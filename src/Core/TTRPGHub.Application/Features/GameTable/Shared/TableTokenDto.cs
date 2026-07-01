namespace TTRPGHub.Features.GameTable.Shared;

public sealed record TableTokenDto(
    Guid Id, string Label, string? ImageUrl, string Color,
    double X, double Y, Guid? OwnerId, bool CanMove);
