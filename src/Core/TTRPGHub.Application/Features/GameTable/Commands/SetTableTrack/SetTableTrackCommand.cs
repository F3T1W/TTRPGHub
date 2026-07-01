using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetTableTrack;

public sealed record SetTableTrackCommand(Guid SessionId, string TrackUrl, string? TrackTitle) : IRequest<Result>;
