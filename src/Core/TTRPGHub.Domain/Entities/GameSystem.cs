using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class GameSystem : Entity<GameSystemId>
{
    public string Slug { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public bool IsOfficial { get; private set; }
    public UserId? CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private init; }

    private GameSystem() { }

    public static GameSystem CreateOfficial(string slug, string name) => new()
    {
        Id = GameSystemId.New(),
        Slug = slug,
        Name = name,
        IsOfficial = true,
        CreatedByUserId = null,
        CreatedAt = DateTime.UtcNow
    };

    public static GameSystem CreateCustom(string slug, string name, UserId createdBy) => new()
    {
        Id = GameSystemId.New(),
        Slug = slug,
        Name = name,
        IsOfficial = false,
        CreatedByUserId = createdBy,
        CreatedAt = DateTime.UtcNow
    };
}
