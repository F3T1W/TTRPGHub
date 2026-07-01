using TTRPGHub.Entities;

namespace TTRPGHub.Entities.Discussions;

public sealed class DiscussionLike
{
    public DiscussionPostId PostId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public DiscussionPost Post { get; private set; } = default!;
    public User User { get; private set; } = default!;

    private DiscussionLike() { }

    public static DiscussionLike Create(DiscussionPostId postId, UserId userId) => new()
    {
        PostId = postId,
        UserId = userId,
        CreatedAt = DateTime.UtcNow,
    };
}
