using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Dnd5e.Monsters.Queries.GetMonsterDetail;

public sealed record GetDnd5eMonsterDetailQuery(Guid Id) : IRequest<Result<MonsterDetailDto>>;

public sealed record MonsterDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string Size,
    string Type,
    string? Subtype,
    string Alignment,
    int ArmorClass,
    string? ArmorDesc,
    int HitPoints,
    string HitDice,
    string Speed,
    int Strength, int Dexterity, int Constitution,
    int Intelligence, int Wisdom, int Charisma,
    string ChallengeRating,
    int Xp,
    string? Senses,
    string? Languages,
    string? Actions,
    string? SpecialAbilities,
    string? Reactions,
    string? LegendaryActions,
    string Source);
