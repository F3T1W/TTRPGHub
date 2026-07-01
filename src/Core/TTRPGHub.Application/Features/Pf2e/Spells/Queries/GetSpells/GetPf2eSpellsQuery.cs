using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;

namespace TTRPGHub.Features.Pf2e.Spells.Queries.GetSpells;

public sealed record GetPf2eSpellsQuery(
    string? Search,
    string? Tradition,
    int? Level,
    string? Trait,
    int Page = 1,
    int PageSize = 30
) : IRequest<Result<PagedResult<Pf2eSpellSummaryDto>>>;

public sealed record Pf2eSpellSummaryDto(
    Guid Id,
    string Name,
    int Level,
    string Traditions,
    string Traits,
    string Cast,
    string? Range,
    string Duration);
