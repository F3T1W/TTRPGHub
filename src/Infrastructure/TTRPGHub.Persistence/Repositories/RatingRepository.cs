using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class RatingRepository(AppDbContext db) : IRatingRepository
{
    public Task<List<UserRating>> GetByRateeAsync(UserId rateeId, CancellationToken ct) =>
        db.UserRatings
            .Include(r => r.Rater)
            .Where(r => r.RateeId == rateeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task<UserRating?> GetByRaterAndRateeAsync(UserId raterId, UserId rateeId, CancellationToken ct) =>
        db.UserRatings.FirstOrDefaultAsync(r => r.RaterId == raterId && r.RateeId == rateeId, ct);

    public Task<UserRating?> GetByIdAsync(UserRatingId id, CancellationToken ct) =>
        db.UserRatings.FindAsync([id], ct).AsTask();

    public async Task AddAsync(UserRating rating, CancellationToken ct) =>
        await db.UserRatings.AddAsync(rating, ct);

    public void Remove(UserRating rating) =>
        db.UserRatings.Remove(rating);

    public async Task<(double Average, int Count)> GetStatsAsync(UserId rateeId, CancellationToken ct)
    {
        var scores = await db.UserRatings
            .Where(r => r.RateeId == rateeId)
            .Select(r => r.Score)
            .ToListAsync(ct);

        return scores.Count == 0
            ? (0, 0)
            : (Math.Round(scores.Average(), 1), scores.Count);
    }
}
