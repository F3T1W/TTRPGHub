using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Rules.Queries.GetRuleEntryDetail;

public sealed record GetRuleEntryDetailQuery(string SystemSlug, RuleCategory Category, string Slug)
    : IRequest<Result<RuleEntryDetailDto>>;

public sealed record RuleEntryDetailDto(
    Guid Id, string SystemSlug, RuleCategory Category, string Slug, string Title,
    string? Summary, string? ContentMarkdown, string StatsJson,
    string[] Tags, bool IsHomebrew, string Source, bool CanEdit);
