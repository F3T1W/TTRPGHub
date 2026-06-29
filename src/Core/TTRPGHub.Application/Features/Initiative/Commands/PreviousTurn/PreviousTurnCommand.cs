using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Initiative.Commands.PreviousTurn;

public sealed record PreviousTurnCommand(Guid TrackerId) : IRequest<Result>;
