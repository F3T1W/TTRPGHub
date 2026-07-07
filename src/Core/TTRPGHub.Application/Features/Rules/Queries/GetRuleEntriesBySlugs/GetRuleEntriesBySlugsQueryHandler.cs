using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Rules.Queries.GetRuleEntriesBySlugs;

internal sealed class GetRuleEntriesBySlugsQueryHandler(
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository
) : IRequestHandler<GetRuleEntriesBySlugsQuery, Result<List<RuleEntryStatsDto>>>
{
    public async Task<Result<List<RuleEntryStatsDto>>> Handle(GetRuleEntriesBySlugsQuery query, CancellationToken ct)
    {
        var system = await systemRepository.GetBySlugAsync(query.SystemSlug, ct);
        if (system is null)
            return Error.NotFound("GameSystem");

        var entries = await entryRepository.GetBySlugsAsync(system.Id, query.Category, query.Slugs, ct);

        return entries
            .Select(e => new RuleEntryStatsDto(e.Slug, e.Title, e.StatsJson))
            .ToList();
    }
}
