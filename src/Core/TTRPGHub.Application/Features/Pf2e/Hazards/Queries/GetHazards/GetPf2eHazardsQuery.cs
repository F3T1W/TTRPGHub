using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;

namespace TTRPGHub.Features.Pf2e.Hazards.Queries.GetHazards;

public sealed record GetPf2eHazardsQuery(
    string? Search,
    int? Level,
    int Page = 1,
    int PageSize = 30
) : IRequest<Result<PagedResult<Pf2eHazardSummaryDto>>>;

public sealed record Pf2eHazardSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    string NameRu,
    int Level,
    string Traits,
    int StealthDc);
