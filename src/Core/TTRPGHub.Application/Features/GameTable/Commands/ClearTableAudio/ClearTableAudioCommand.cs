using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.ClearTableAudio;

public sealed record ClearTableAudioCommand(Guid SessionId) : IRequest<Result>;
