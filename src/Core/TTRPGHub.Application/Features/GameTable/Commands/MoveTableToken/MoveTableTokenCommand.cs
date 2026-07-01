using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.MoveTableToken;

public sealed record MoveTableTokenCommand(Guid SessionId, Guid TokenId, double X, double Y) : IRequest<Result>;
