using TTRPGHub.Common;

namespace TTRPGHub.Entities.Forum;

public sealed class ForumPost : Entity<ForumPostId>
{
    public ForumTopicId TopicId { get; private set; }
    public UserId AuthorId { get; private set; }
    public string Content { get; private set; } = default!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public ForumTopic Topic { get; private set; } = default!;
    public User Author { get; private set; } = default!;

    private readonly List<ForumPostLike> _likes = [];
    public IReadOnlyList<ForumPostLike> Likes => _likes.AsReadOnly();

    private ForumPost() { }

    public static ForumPost Create(ForumTopicId topicId, UserId authorId, string content)
    {
        return new ForumPost
        {
            Id = ForumPostId.New(),
            TopicId = topicId,
            AuthorId = authorId,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateContent(string content)
    {
        Content = content;
        UpdatedAt = DateTime.UtcNow;
    }
}
