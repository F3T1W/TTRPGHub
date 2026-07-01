using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.RemoveTableToken;

public sealed record RemoveTableTokenCommand(Guid SessionId, Guid TokenId) : IRequest<Result>;
