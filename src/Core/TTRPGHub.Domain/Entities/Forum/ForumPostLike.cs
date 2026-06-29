namespace TTRPGHub.Entities.Forum;

public sealed class ForumPostLike
{
    public ForumPostId PostId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public ForumPost Post { get; private set; } = default!;
    public User User { get; private set; } = default!;

    private ForumPostLike() { }

    public static ForumPostLike Create(ForumPostId postId, UserId userId) =>
        new() { PostId = postId, UserId = userId, CreatedAt = DateTime.UtcNow };
}
