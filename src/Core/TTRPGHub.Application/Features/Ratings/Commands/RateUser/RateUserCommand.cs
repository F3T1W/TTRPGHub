using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Ratings.Commands.RateUser;

public sealed record RateUserCommand(Guid RateeId, int Score, string? Comment, string Role)
    : IRequest<Result<Guid>>;
