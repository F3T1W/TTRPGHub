using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.PauseTableAudio;

public sealed record PauseTableAudioCommand(Guid SessionId, double PositionSeconds) : IRequest<Result>;
