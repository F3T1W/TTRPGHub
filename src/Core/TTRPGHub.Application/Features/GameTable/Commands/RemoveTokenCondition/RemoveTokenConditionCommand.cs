using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.RemoveTokenCondition;

public sealed record RemoveTokenConditionCommand(Guid SessionId, Guid TokenId, string Slug) : IRequest<Result>;
