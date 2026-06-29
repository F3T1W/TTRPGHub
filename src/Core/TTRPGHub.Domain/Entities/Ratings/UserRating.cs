using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Entities.Ratings;

public sealed class UserRating : Entity<UserRatingId>
{
    public UserId RaterId { get; private set; }
    public UserId RateeId { get; private set; }
    public int Score { get; private set; }
    public string? Comment { get; private set; }
    public RatingRole Role { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public User? Rater { get; private set; }
    public User? Ratee { get; private set; }

    private UserRating() { }

    public static UserRating Create(UserId raterId, UserId rateeId, int score, string? comment, RatingRole role) =>
        new()
        {
            Id = UserRatingId.New(),
            RaterId = raterId,
            RateeId = rateeId,
            Score = score,
            Comment = comment,
            Role = role,
            CreatedAt = DateTime.UtcNow
        };

    public void Update(int score, string? comment, RatingRole role)
    {
        Score = score;
        Comment = comment;
        Role = role;
        UpdatedAt = DateTime.UtcNow;
    }
}
