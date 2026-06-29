using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;
using TTRPGHub.Repositories.Dnd5e;

namespace TTRPGHub.Features.Dnd5e.Monsters.Queries.GetMonsters;

internal sealed class GetDnd5eMonstersQueryHandler(IDnd5eMonsterRepository repository)
    : IRequestHandler<GetDnd5eMonstersQuery, Result<PagedResult<MonsterSummaryDto>>>
{
    public async Task<Result<PagedResult<MonsterSummaryDto>>> Handle(
        GetDnd5eMonstersQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.SearchAsync(
            query.Search, query.Type, query.Size, query.Cr,
            query.Page, query.PageSize, ct);

        var dtos = items.Select(m => new MonsterSummaryDto(
            m.Id.Value, m.Name, m.Size, m.Type, m.Subtype,
            m.Alignment, m.ArmorClass, m.HitPoints,
            m.ChallengeRating, m.Xp)).ToList();

        return new PagedResult<MonsterSummaryDto>(dtos, total, query.Page, query.PageSize);
    }
}
