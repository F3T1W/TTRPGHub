using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Entities.Discussions;

public sealed class DiscussionPost : Entity<DiscussionPostId>
{
    public DiscussionEntityType EntityType { get; private set; }
    public string EntitySlug { get; private set; } = default!;
    public UserId AuthorId { get; private set; }
    public string Content { get; private set; } = default!;
    public DiscussionPostId? ParentId { get; private set; }
    public int LikeCount { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User Author { get; private set; } = default!;

    private readonly List<DiscussionLike> _likes = [];
    public IReadOnlyList<DiscussionLike> Likes => _likes.AsReadOnly();

    private DiscussionPost() { }

    public static DiscussionPost Create(
        DiscussionEntityType entityType,
        string entitySlug,
        UserId authorId,
        string content,
        DiscussionPostId? parentId = null) => new()
    {
        Id = DiscussionPostId.New(),
        EntityType = entityType,
        EntitySlug = entitySlug.ToLowerInvariant(),
        AuthorId = authorId,
        Content = content,
        ParentId = parentId,
        LikeCount = 0,
        CreatedAt = DateTime.UtcNow,
    };

    public void AddLike() => LikeCount++;
    public void RemoveLike() => LikeCount = Math.Max(0, LikeCount - 1);
}
