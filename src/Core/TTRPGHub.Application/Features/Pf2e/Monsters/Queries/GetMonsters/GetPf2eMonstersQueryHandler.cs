using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Pf2e.Monsters.Queries.GetMonsters;

internal sealed class GetPf2eMonstersQueryHandler(IPf2eMonsterRepository repository)
    : IRequestHandler<GetPf2eMonstersQuery, Result<PagedResult<Pf2eMonsterSummaryDto>>>
{
    public async Task<Result<PagedResult<Pf2eMonsterSummaryDto>>> Handle(
        GetPf2eMonstersQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.SearchAsync(
            query.Search, query.Trait, query.Size, query.Level,
            query.Page, query.PageSize, ct);

        var dtos = items.Select(m => new Pf2eMonsterSummaryDto(
            m.Id.Value, m.Name, m.Level, m.Size, m.Traits,
            m.ArmorClass, m.HitPoints)).ToList();

        return new PagedResult<Pf2eMonsterSummaryDto>(dtos, total, query.Page, query.PageSize);
    }
}
