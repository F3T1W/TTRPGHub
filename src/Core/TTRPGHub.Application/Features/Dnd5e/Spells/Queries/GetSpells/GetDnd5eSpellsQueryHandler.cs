using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories.Dnd5e;

namespace TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;

internal sealed class GetDnd5eSpellsQueryHandler(IDnd5eSpellRepository repository)
    : IRequestHandler<GetDnd5eSpellsQuery, Result<PagedResult<SpellSummaryDto>>>
{
    public async Task<Result<PagedResult<SpellSummaryDto>>> Handle(
        GetDnd5eSpellsQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.SearchAsync(
            query.Search, query.School, query.Level, query.ClassName,
            query.Page, query.PageSize, ct);

        var dtos = items.Select(s => new SpellSummaryDto(
            s.Id.Value, s.Name, s.Level, s.School,
            s.CastingTime, s.Range, s.Duration,
            s.Concentration, s.Ritual, s.Classes)).ToList();

        return new PagedResult<SpellSummaryDto>(dtos, total, query.Page, query.PageSize);
    }
}
