using TTRPGHub.Entities;
using TTRPGHub.Entities.Ratings;

namespace TTRPGHub.Repositories;

public interface ISessionReviewRepository
{
    Task<List<SessionReview>> GetBySessionAsync(GameSessionId sessionId, CancellationToken ct = default);
    Task<List<SessionReview>> GetByRevieweeAsync(UserId revieweeId, CancellationToken ct = default);
    Task<SessionReview?> GetBySessionReviewerRevieweeAsync(
        GameSessionId sessionId, UserId reviewerId, UserId revieweeId, CancellationToken ct = default);
    Task<SessionReview?> GetByIdAsync(SessionReviewId id, CancellationToken ct = default);
    Task AddAsync(SessionReview review, CancellationToken ct = default);
    void Remove(SessionReview review);
    Task<(double Average, int Count)> GetStatsAsync(UserId revieweeId, CancellationToken ct = default);
}
