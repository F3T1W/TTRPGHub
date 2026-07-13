using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class SessionReviewRepository(AppDbContext db) : ISessionReviewRepository
{
    public Task<List<SessionReview>> GetBySessionAsync(GameSessionId sessionId, CancellationToken ct = default) =>
        db.SessionReviews
            .Include(r => r.Reviewer)
            .Where(r => r.SessionId == sessionId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task<List<SessionReview>> GetByRevieweeAsync(UserId revieweeId, CancellationToken ct = default) =>
        db.SessionReviews
            .Include(r => r.Reviewer)
            .Where(r => r.RevieweeId == revieweeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task<SessionReview?> GetBySessionReviewerRevieweeAsync(
        GameSessionId sessionId, UserId reviewerId, UserId revieweeId, CancellationToken ct = default) =>
        db.SessionReviews.FirstOrDefaultAsync(
            r => r.SessionId == sessionId && r.ReviewerId == reviewerId && r.RevieweeId == revieweeId, ct);

    public Task<SessionReview?> GetByIdAsync(SessionReviewId id, CancellationToken ct = default) =>
        db.SessionReviews.FindAsync([id], ct).AsTask();

    public async Task AddAsync(SessionReview review, CancellationToken ct = default) =>
        await db.SessionReviews.AddAsync(review, ct);

    public void Remove(SessionReview review) =>
        db.SessionReviews.Remove(review);

    public async Task<(double Average, int Count)> GetStatsAsync(UserId revieweeId, CancellationToken ct = default)
    {
        var scores = await db.SessionReviews
            .Where(r => r.RevieweeId == revieweeId)
            .Select(r => r.Score)
            .ToListAsync(ct);

        return scores.Count == 0
            ? (0, 0)
            : (Math.Round(scores.Average(), 1), scores.Count);
    }
}
