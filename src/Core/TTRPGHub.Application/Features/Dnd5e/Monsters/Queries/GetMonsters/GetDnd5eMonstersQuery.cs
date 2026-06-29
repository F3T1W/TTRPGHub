using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;

namespace TTRPGHub.Features.Dnd5e.Monsters.Queries.GetMonsters;

public sealed record GetDnd5eMonstersQuery(
    string? Search,
    string? Type,
    string? Size,
    string? Cr,
    int Page = 1,
    int PageSize = 30
) : IRequest<Result<PagedResult<MonsterSummaryDto>>>;

public sealed record MonsterSummaryDto(
    Guid Id,
    string Name,
    string Size,
    string Type,
    string? Subtype,
    string Alignment,
    int ArmorClass,
    int HitPoints,
    string ChallengeRating,
    int Xp);
