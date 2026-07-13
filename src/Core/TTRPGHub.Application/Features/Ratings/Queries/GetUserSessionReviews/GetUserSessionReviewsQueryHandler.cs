using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Ratings.Queries.GetUserSessionReviews;

internal sealed class GetUserSessionReviewsQueryHandler(
    ISessionReviewRepository reviews,
    IGameSessionRepository sessions
) : IRequestHandler<GetUserSessionReviewsQuery, Result<UserSessionReviewsResult>>
{
    public async Task<Result<UserSessionReviewsResult>> Handle(GetUserSessionReviewsQuery request, CancellationToken ct)
    {
        var revieweeId = new UserId(request.RevieweeId);
        var list = await reviews.GetByRevieweeAsync(revieweeId, ct);
        var (avg, count) = await reviews.GetStatsAsync(revieweeId, ct);

        var result = new List<UserSessionReviewDto>(list.Count);
        foreach (var r in list)
        {
            var session = await sessions.GetByIdAsync(r.SessionId, ct);
            result.Add(new UserSessionReviewDto(
                r.Id.Value, r.SessionId.Value, session?.Title ?? "—",
                r.ReviewerId.Value, r.Reviewer?.Username ?? "—",
                r.Score, r.Comment, r.CreatedAt));
        }

        return new UserSessionReviewsResult(result, avg, count);
    }
}
