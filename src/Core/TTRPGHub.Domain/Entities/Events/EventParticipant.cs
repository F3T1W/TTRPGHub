using TTRPGHub.Entities;

namespace TTRPGHub.Entities.Events;

public sealed class EventParticipant
{
    public GameEventId EventId { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime RegisteredAt { get; private set; }

    public GameEvent? Event { get; private set; }
    public User? User { get; private set; }

    private EventParticipant() { }

    public static EventParticipant Create(GameEventId eventId, UserId userId) =>
        new() { EventId = eventId, UserId = userId, RegisteredAt = DateTime.UtcNow };
}
