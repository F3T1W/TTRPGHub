using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Initiative.Commands.NextTurn;

public sealed record NextTurnCommand(Guid TrackerId) : IRequest<Result>;
