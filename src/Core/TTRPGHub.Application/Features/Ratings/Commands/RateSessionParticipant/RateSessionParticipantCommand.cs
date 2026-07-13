using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Ratings.Commands.RateSessionParticipant;

public sealed record RateSessionParticipantCommand(
    Guid SessionId, Guid RevieweeId, int Score, string? Comment) : IRequest<Result<Guid>>;
