using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;

namespace TTRPGHub.Repositories;

public interface IRatingRepository
{
    Task<List<UserRating>> GetByRateeAsync(UserId rateeId, CancellationToken ct = default);
    Task<UserRating?> GetByRaterAndRateeAsync(UserId raterId, UserId rateeId, CancellationToken ct = default);
    Task<UserRating?> GetByIdAsync(UserRatingId id, CancellationToken ct = default);
    Task AddAsync(UserRating rating, CancellationToken ct = default);
    void Remove(UserRating rating);
    Task<(double Average, int Count)> GetStatsAsync(UserId rateeId, CancellationToken ct = default);
}
