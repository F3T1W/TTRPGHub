using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.ApplyTokenCondition;

public sealed record ApplyTokenConditionCommand(
    Guid SessionId, Guid TokenId, string Slug, string Name, int? Value
) : IRequest<Result>;
