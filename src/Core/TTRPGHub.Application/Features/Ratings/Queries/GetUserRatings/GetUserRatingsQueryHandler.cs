using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Ratings.Queries.GetUserRatings;

internal sealed class GetUserRatingsQueryHandler(IRatingRepository ratings)
    : IRequestHandler<GetUserRatingsQuery, Result<UserRatingsResult>>
{
    public async Task<Result<UserRatingsResult>> Handle(GetUserRatingsQuery request, CancellationToken ct)
    {
        var rateeId = new UserId(request.RateeId);
        var list = await ratings.GetByRateeAsync(rateeId, ct);
        var (avg, count) = await ratings.GetStatsAsync(rateeId, ct);

        var dtos = list.Select(r => new UserRatingDto(
            r.Id.Value,
            r.RaterId.Value,
            r.Rater?.Username ?? "?",
            r.Rater?.Profile.AvatarUrl,
            r.Score,
            r.Comment,
            r.Role.ToString(),
            r.CreatedAt)).ToList();

        return Result<UserRatingsResult>.Success(new UserRatingsResult(dtos, avg, count));
    }
}
