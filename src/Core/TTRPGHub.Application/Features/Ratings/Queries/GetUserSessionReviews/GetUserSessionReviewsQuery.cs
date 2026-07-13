using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Ratings.Queries.GetUserSessionReviews;

public sealed record GetUserSessionReviewsQuery(Guid RevieweeId) : IRequest<Result<UserSessionReviewsResult>>;

public sealed record UserSessionReviewDto(
    Guid Id, Guid SessionId, string SessionTitle, Guid ReviewerId, string ReviewerUsername,
    int Score, string? Comment, DateTime CreatedAt);

public sealed record UserSessionReviewsResult(
    List<UserSessionReviewDto> Reviews, double AverageScore, int TotalCount);
