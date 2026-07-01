using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Rules.Queries.GetRuleEntryDetail;

internal sealed class GetRuleEntryDetailQueryHandler(
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository,
    ICurrentUser currentUser
) : IRequestHandler<GetRuleEntryDetailQuery, Result<RuleEntryDetailDto>>
{
    public async Task<Result<RuleEntryDetailDto>> Handle(GetRuleEntryDetailQuery query, CancellationToken ct)
    {
        var system = await systemRepository.GetBySlugAsync(query.SystemSlug, ct);
        if (system is null)
            return Error.NotFound("GameSystem");

        var entry = await entryRepository.GetBySlugAsync(system.Id, query.Category, query.Slug, ct);
        if (entry is null)
            return Error.NotFound("RuleEntry");

        var canEdit = !system.IsOfficial && system.CreatedByUserId == currentUser.Id;

        return new RuleEntryDetailDto(
            entry.Id.Value, query.SystemSlug, entry.Category, entry.Slug, entry.Title,
            entry.Summary, entry.ContentMarkdown, entry.StatsJson,
            entry.Tags, entry.IsHomebrew, entry.Source, canEdit);
    }
}
