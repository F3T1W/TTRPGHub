namespace TTRPGHub.Entities.Homebrew;

public sealed class HomebrewLike
{
    public HomebrewItemId ItemId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public HomebrewItem Item { get; private set; } = default!;
    public User User { get; private set; } = default!;

    private HomebrewLike() { }

    public static HomebrewLike Create(HomebrewItemId itemId, UserId userId) =>
        new() { ItemId = itemId, UserId = userId, CreatedAt = DateTime.UtcNow };
}
