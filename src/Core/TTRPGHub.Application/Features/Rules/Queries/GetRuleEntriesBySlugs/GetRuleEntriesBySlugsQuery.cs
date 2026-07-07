using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Rules.Queries.GetRuleEntriesBySlugs;

public sealed record GetRuleEntriesBySlugsQuery(
    string SystemSlug, RuleCategory Category, IReadOnlyCollection<string> Slugs
) : IRequest<Result<List<RuleEntryStatsDto>>>;

public sealed record RuleEntryStatsDto(string Slug, string Title, string StatsJson);
