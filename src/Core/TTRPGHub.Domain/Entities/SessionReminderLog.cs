namespace TTRPGHub.Entities;

public sealed class SessionReminderLog
{
    public GameSessionId SessionId { get; private init; }
    public UserId UserId { get; private init; }
    public DateTime SentAt { get; private init; }

    private SessionReminderLog() { }

    public static SessionReminderLog Create(GameSessionId sessionId, UserId userId) => new()
    {
        SessionId = sessionId,
        UserId = userId,
        SentAt = DateTime.UtcNow
    };
}
