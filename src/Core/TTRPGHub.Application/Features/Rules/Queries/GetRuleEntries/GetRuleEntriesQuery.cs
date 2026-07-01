using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Rules.Queries.GetRuleEntries;

public sealed record GetRuleEntriesQuery(
    string SystemSlug, RuleCategory Category, string? Search,
    int Page = 1, int PageSize = 40
) : IRequest<Result<RuleEntryPageDto>>;

public sealed record RuleEntrySummaryDto(Guid Id, string Slug, string Title, string? Summary, string[] Tags);

public sealed record RuleEntryPageDto(List<RuleEntrySummaryDto> Items, int Total, int Page, int PageSize);
