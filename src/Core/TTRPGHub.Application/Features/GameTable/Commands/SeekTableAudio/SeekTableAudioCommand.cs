using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SeekTableAudio;

public sealed record SeekTableAudioCommand(Guid SessionId, double PositionSeconds) : IRequest<Result>;
