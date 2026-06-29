using TTRPGHub.Common;

namespace TTRPGHub.Entities.Forum;

public sealed class ForumCategory : Entity<ForumCategoryId>
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public int DisplayOrder { get; private set; }

    private readonly List<ForumTopic> _topics = [];
    public IReadOnlyList<ForumTopic> Topics => _topics.AsReadOnly();

    private ForumCategory() { }

    public static ForumCategory Create(string name, string description, string slug, int displayOrder = 0)
    {
        return new ForumCategory
        {
            Id = ForumCategoryId.New(),
            Name = name,
            Description = description,
            Slug = slug,
            DisplayOrder = displayOrder
        };
    }
}
