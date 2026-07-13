namespace TTRPGHub.Features.GameTable.Shared;

public sealed record TableTokenDto(
    Guid Id, string Label, string? ImageUrl, string Color,
    double X, double Y, int Width, int Height, int Rotation, Guid? OwnerId, bool CanMove,
    string CombatantType, Guid? CombatantId, int? CurrentHp, int? MaxHp, int? ArmorClass,
    List<TokenConditionDto> Conditions, int? Initiative, bool HasDarkvision, bool HasLowLightVision,
    List<Guid>? VisibleToUserIds,
    int? CurrentStamina = null, int? MaxStamina = null,
    List<Guid>? CoOwnerIds = null);

public sealed record TokenConditionDto(Guid Id, string Slug, string Name, int? Value);
