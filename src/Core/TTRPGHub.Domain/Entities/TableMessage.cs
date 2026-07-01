namespace TTRPGHub.Entities;

public sealed class TableMessage
{
    public Guid Id { get; private init; }
    public GameSessionId SessionId { get; private init; }
    public UserId SenderId { get; private init; }
    public string SenderUsername { get; private init; } = null!;
    public UserId? RecipientId { get; private init; }
    public string? RecipientUsername { get; private init; }
    public TableMessageKind Kind { get; private init; }
    public string Content { get; private init; } = null!;
    public DateTime CreatedAt { get; private init; }

    private TableMessage() { }

    public static TableMessage CreateChat(GameSessionId sessionId, UserId senderId, string senderUsername, string content) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        SenderId = senderId,
        SenderUsername = senderUsername,
        Kind = TableMessageKind.Chat,
        Content = content,
        CreatedAt = DateTime.UtcNow
    };

    public static TableMessage CreateRoll(GameSessionId sessionId, UserId senderId, string senderUsername, string content) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        SenderId = senderId,
        SenderUsername = senderUsername,
        Kind = TableMessageKind.Roll,
        Content = content,
        CreatedAt = DateTime.UtcNow
    };

    public static TableMessage CreateSystem(GameSessionId sessionId, string content) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        SenderId = default,
        SenderUsername = "Система",
        Kind = TableMessageKind.System,
        Content = content,
        CreatedAt = DateTime.UtcNow
    };

    public static TableMessage CreateWhisper(
        GameSessionId sessionId, UserId senderId, string senderUsername,
        UserId recipientId, string recipientUsername, string content) => new()
    {
        Id = Guid.NewGuid(),
        SessionId = sessionId,
        SenderId = senderId,
        SenderUsername = senderUsername,
        RecipientId = recipientId,
        RecipientUsername = recipientUsername,
        Kind = TableMessageKind.Whisper,
        Content = content,
        CreatedAt = DateTime.UtcNow
    };

    public bool IsVisibleTo(UserId userId) =>
        Kind != TableMessageKind.Whisper || SenderId == userId || RecipientId == userId;
}

public enum TableMessageKind { Chat, Roll, System, Whisper }
