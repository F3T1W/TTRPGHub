namespace TTRPGHub.Entities;

public sealed class TableToken
{
    public Guid Id { get; private init; }
    public GameSessionId SessionId { get; private init; }
    public string Label { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public string Color { get; private set; } = null!;
    public double X { get; private set; }
    public double Y { get; private set; }
    public UserId? OwnerId { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private TableToken() { }

    public static TableToken Create(
        GameSessionId sessionId, string label, string? imageUrl, string color,
        double x, double y, UserId? ownerId)
    {
        var now = DateTime.UtcNow;
        return new TableToken
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Label = label,
            ImageUrl = imageUrl,
            Color = color,
            X = Clamp(x),
            Y = Clamp(y),
            OwnerId = ownerId,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public bool CanBeMovedBy(UserId userId, bool isOrganizer) =>
        isOrganizer || OwnerId == userId;

    public void Move(double x, double y)
    {
        X = Clamp(x);
        Y = Clamp(y);
        UpdatedAt = DateTime.UtcNow;
    }

    private static double Clamp(double value) => Math.Clamp(value, 0d, 1d);
}
