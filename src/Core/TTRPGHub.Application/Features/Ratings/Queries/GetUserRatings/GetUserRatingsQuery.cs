using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Ratings.Queries.GetUserRatings;

public sealed record GetUserRatingsQuery(Guid RateeId) : IRequest<Result<UserRatingsResult>>;

public sealed record UserRatingDto(
    Guid Id, Guid RaterId, string RaterUsername, string? RaterAvatarUrl,
    int Score, string? Comment, string Role, DateTime CreatedAt);

public sealed record UserRatingsResult(
    List<UserRatingDto> Ratings, double AverageScore, int TotalCount);
