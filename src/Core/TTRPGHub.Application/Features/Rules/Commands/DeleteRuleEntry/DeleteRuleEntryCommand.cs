using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Rules.Commands.DeleteRuleEntry;

public sealed record DeleteRuleEntryCommand(string SystemSlug, RuleCategory Category, string Slug) : IRequest<Result>;
