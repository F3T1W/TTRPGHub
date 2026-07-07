using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Pf2e.Spells.Queries.GetSpells;

internal sealed class GetPf2eSpellsQueryHandler(IPf2eSpellRepository repository)
    : IRequestHandler<GetPf2eSpellsQuery, Result<PagedResult<Pf2eSpellSummaryDto>>>
{
    public async Task<Result<PagedResult<Pf2eSpellSummaryDto>>> Handle(
        GetPf2eSpellsQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.SearchAsync(
            query.Search, query.Tradition, query.Level, query.Trait,
            query.Page, query.PageSize, ct);

        var dtos = items.Select(s => new Pf2eSpellSummaryDto(
            s.Id.Value, s.Slug, s.Name, s.Level, s.Traditions, s.Traits,
            s.Cast, s.Range, s.Duration)).ToList();

        return new PagedResult<Pf2eSpellSummaryDto>(dtos, total, query.Page, query.PageSize);
    }
}
