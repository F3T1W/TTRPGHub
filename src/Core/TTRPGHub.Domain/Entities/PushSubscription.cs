namespace TTRPGHub.Entities;

public sealed class PushSubscription
{
    public Guid Id { get; private init; }
    public UserId UserId { get; private init; }
    public string Endpoint { get; private init; } = null!;
    public string P256dh { get; private init; } = null!;
    public string Auth { get; private init; } = null!;
    public DateTime CreatedAt { get; private init; }

    private PushSubscription() { }

    public static PushSubscription Create(UserId userId, string endpoint, string p256dh, string auth) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Endpoint = endpoint,
        P256dh = p256dh,
        Auth = auth,
        CreatedAt = DateTime.UtcNow
    };
}
