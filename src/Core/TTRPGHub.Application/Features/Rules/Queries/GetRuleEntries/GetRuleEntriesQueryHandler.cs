using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Rules.Queries.GetRuleEntries;

internal sealed class GetRuleEntriesQueryHandler(
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository
) : IRequestHandler<GetRuleEntriesQuery, Result<RuleEntryPageDto>>
{
    public async Task<Result<RuleEntryPageDto>> Handle(GetRuleEntriesQuery query, CancellationToken ct)
    {
        var system = await systemRepository.GetBySlugAsync(query.SystemSlug, ct);
        if (system is null)
            return Error.NotFound("GameSystem");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var entries = await entryRepository.SearchAsync(system.Id, query.Category, query.Search, page, pageSize, ct);
        var total = await entryRepository.CountAsync(system.Id, query.Category, query.Search, ct);

        var items = entries
            .Select(e => new RuleEntrySummaryDto(e.Id.Value, e.Slug, e.Title, e.Summary, e.Tags))
            .ToList();

        return new RuleEntryPageDto(items, total, page, pageSize);
    }
}
