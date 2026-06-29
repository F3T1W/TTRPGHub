using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Ratings.Commands.DeleteRating;

public sealed record DeleteRatingCommand(Guid RatingId) : IRequest<Result>;
