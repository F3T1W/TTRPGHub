using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Rules.Commands.CreateRuleEntry;

public sealed record CreateRuleEntryCommand(
    string SystemSlug, RuleCategory Category, string Title,
    string? Summary, string? ContentMarkdown, string StatsJson, string[] Tags
) : IRequest<Result<CreateRuleEntryResponse>>;

public sealed record CreateRuleEntryResponse(Guid Id, string Slug);
