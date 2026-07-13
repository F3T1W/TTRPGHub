using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Ratings.Commands.DeleteSessionReview;

public sealed record DeleteSessionReviewCommand(Guid ReviewId) : IRequest<Result>;
