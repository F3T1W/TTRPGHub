using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Ratings.Queries.GetSessionReviews;

public sealed record GetSessionReviewsQuery(Guid SessionId) : IRequest<Result<List<SessionReviewDto>>>;

public sealed record SessionReviewDto(
    Guid Id, Guid ReviewerId, string ReviewerUsername, Guid RevieweeId, string RevieweeUsername,
    int Score, string? Comment, DateTime CreatedAt);
