using TTRPGHub.Common;

namespace TTRPGHub.Entities.Forum;

public sealed class ForumTopic : Entity<ForumTopicId>
{
    public ForumCategoryId CategoryId { get; private set; }
    public UserId AuthorId { get; private set; }
    public string Title { get; private set; } = default!;
    public bool IsPinned { get; private set; }
    public bool IsLocked { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastPostAt { get; private set; }

    public ForumCategory Category { get; private set; } = default!;
    public User Author { get; private set; } = default!;

    private readonly List<ForumPost> _posts = [];
    public IReadOnlyList<ForumPost> Posts => _posts.AsReadOnly();

    private ForumTopic() { }

    public static ForumTopic Create(ForumCategoryId categoryId, UserId authorId, string title)
    {
        return new ForumTopic
        {
            Id = ForumTopicId.New(),
            CategoryId = categoryId,
            AuthorId = authorId,
            Title = title,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void UpdateLastPostAt(DateTime at) => LastPostAt = at;
    public void Pin() => IsPinned = true;
    public void Unpin() => IsPinned = false;
    public void Lock() => IsLocked = true;
    public void Unlock() => IsLocked = false;
}
