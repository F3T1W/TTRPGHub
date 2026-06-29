using TTRPGHub.Common;

namespace TTRPGHub.Entities.Homebrew;

public sealed class HomebrewItem : Entity<HomebrewItemId>
{
    public UserId AuthorId { get; private set; }
    public string Title { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string System { get; private set; } = default!;
    public HomebrewType Type { get; private set; }
    public string Content { get; private set; } = default!;
    public string Tags { get; private set; } = "";
    public bool IsPublished { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public User Author { get; private set; } = default!;

    private readonly List<HomebrewLike> _likes = [];
    public IReadOnlyList<HomebrewLike> Likes => _likes.AsReadOnly();

    private HomebrewItem() { }

    public static HomebrewItem Create(
        UserId authorId, string title, string description,
        string system, HomebrewType type, string content, string tags)
    {
        return new HomebrewItem
        {
            Id = HomebrewItemId.New(),
            AuthorId = authorId,
            Title = title,
            Description = description,
            System = system,
            Type = type,
            Content = content,
            Tags = tags,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string title, string description, string system, HomebrewType type, string content, string tags)
    {
        Title = title;
        Description = description;
        System = system;
        Type = type;
        Content = content;
        Tags = tags;
        UpdatedAt = DateTime.UtcNow;
    }
}
