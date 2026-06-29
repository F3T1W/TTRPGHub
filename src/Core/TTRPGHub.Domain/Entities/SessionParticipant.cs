namespace TTRPGHub.Entities;

public sealed class SessionParticipant
{
    public UserId UserId { get; private init; }
    public GameSessionId SessionId { get; private init; }
    public ParticipantRole Role { get; private set; }
    public DateTime JoinedAt { get; private init; }

    private SessionParticipant() { }

    public static SessionParticipant Create(UserId userId, GameSessionId sessionId, ParticipantRole role) =>
        new() { UserId = userId, SessionId = sessionId, Role = role, JoinedAt = DateTime.UtcNow };
}

public enum ParticipantRole { Player, DungeonMaster }
