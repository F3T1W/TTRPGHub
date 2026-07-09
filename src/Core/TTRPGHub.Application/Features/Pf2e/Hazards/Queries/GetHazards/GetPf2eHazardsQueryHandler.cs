using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Pf2e.Hazards.Queries.GetHazards;

internal sealed class GetPf2eHazardsQueryHandler(IPf2eHazardRepository repository)
    : IRequestHandler<GetPf2eHazardsQuery, Result<PagedResult<Pf2eHazardSummaryDto>>>
{
    public async Task<Result<PagedResult<Pf2eHazardSummaryDto>>> Handle(
        GetPf2eHazardsQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.SearchAsync(
            query.Search, query.Level, query.Page, query.PageSize, ct);

        var dtos = items.Select(h => new Pf2eHazardSummaryDto(
            h.Id.Value, h.Slug, h.Name, h.NameRu, h.Level, h.Traits, h.StealthDc)).ToList();

        return new PagedResult<Pf2eHazardSummaryDto>(dtos, total, query.Page, query.PageSize);
    }
}
