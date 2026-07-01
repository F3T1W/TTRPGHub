using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Rules.Commands.UpdateRuleEntry;

public sealed record UpdateRuleEntryCommand(
    string SystemSlug, RuleCategory Category, string Slug,
    string Title, string? Summary, string? ContentMarkdown, string StatsJson, string[] Tags
) : IRequest<Result>;
