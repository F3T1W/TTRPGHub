using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;

namespace TTRPGHub.Features.Pf2e.Monsters.Queries.GetMonsters;

public sealed record GetPf2eMonstersQuery(
    string? Search,
    string? Trait,
    string? Size,
    int? Level,
    int Page = 1,
    int PageSize = 30
) : IRequest<Result<PagedResult<Pf2eMonsterSummaryDto>>>;

public sealed record Pf2eMonsterSummaryDto(
    Guid Id,
    string Name,
    int Level,
    string Size,
    string Traits,
    int ArmorClass,
    int HitPoints);
