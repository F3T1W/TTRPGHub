using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.PlayTableAudio;

public sealed record PlayTableAudioCommand(Guid SessionId, double PositionSeconds) : IRequest<Result>;
