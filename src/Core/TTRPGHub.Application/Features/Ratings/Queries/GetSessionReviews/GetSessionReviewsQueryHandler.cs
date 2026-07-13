using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Ratings.Queries.GetSessionReviews;

internal sealed class GetSessionReviewsQueryHandler(
    ISessionReviewRepository reviews,
    IUserRepository users
) : IRequestHandler<GetSessionReviewsQuery, Result<List<SessionReviewDto>>>
{
    public async Task<Result<List<SessionReviewDto>>> Handle(GetSessionReviewsQuery request, CancellationToken ct)
    {
        var list = await reviews.GetBySessionAsync(new GameSessionId(request.SessionId), ct);
        var result = new List<SessionReviewDto>(list.Count);

        foreach (var r in list)
        {
            var reviewee = await users.GetByIdAsync(r.RevieweeId, ct);
            result.Add(new SessionReviewDto(
                r.Id.Value, r.ReviewerId.Value, r.Reviewer?.Username ?? "—",
                r.RevieweeId.Value, reviewee?.Username ?? "—",
                r.Score, r.Comment, r.CreatedAt));
        }

        return result;
    }
}
