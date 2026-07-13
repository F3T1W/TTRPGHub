using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Entities.Ratings;

// Отзыв конкретно по сыгранной сессии — в отличие от UserRating (один общий отзыв на пару
// пользователей, обновляется по (RaterId, RateeId)), здесь одна пара может оставить отзыв за
// каждую сыгранную вместе сессию отдельно: (SessionId, ReviewerId, RevieweeId) уникальны вместе,
// а не (ReviewerId, RevieweeId) сами по себе. Это и есть весь смысл функции — оценить конкретную
// игру, а не общее впечатление о человеке.
public sealed class SessionReview : Entity<SessionReviewId>
{
    public GameSessionId SessionId { get; private set; }
    public UserId ReviewerId { get; private set; }
    public UserId RevieweeId { get; private set; }
    public int Score { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public User? Reviewer { get; private set; }
    public User? Reviewee { get; private set; }

    private SessionReview() { }

    public static SessionReview Create(GameSessionId sessionId, UserId reviewerId, UserId revieweeId, int score, string? comment) =>
        new()
        {
            Id = SessionReviewId.New(),
            SessionId = sessionId,
            ReviewerId = reviewerId,
            RevieweeId = revieweeId,
            Score = score,
            Comment = comment,
            CreatedAt = DateTime.UtcNow
        };

    public void Update(int score, string? comment)
    {
        Score = score;
        Comment = comment;
        UpdatedAt = DateTime.UtcNow;
    }
}
