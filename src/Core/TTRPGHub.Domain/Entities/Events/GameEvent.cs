using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Entities.Events;

public sealed class GameEvent : Entity<GameEventId>
{
    public UserId OrganizerId { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string System { get; private set; } = null!;
    public EventFormat Format { get; private set; }
    public string? Location { get; private set; }
    public string? OnlineLink { get; private set; }
    public DateTime StartsAt { get; private set; }
    public int MaxParticipants { get; private set; }
    public bool IsCancelled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User? Organizer { get; private set; }

    private readonly List<EventParticipant> _participants = [];
    public IReadOnlyList<EventParticipant> Participants => _participants.AsReadOnly();

    private GameEvent() { }

    public static GameEvent Create(
        UserId organizerId, string title, string? description, string system,
        EventFormat format, string? location, string? onlineLink,
        DateTime startsAt, int maxParticipants)
    {
        var now = DateTime.UtcNow;
        return new GameEvent
        {
            Id = GameEventId.New(),
            OrganizerId = organizerId,
            Title = title,
            Description = description,
            System = system,
            Format = format,
            Location = location,
            OnlineLink = onlineLink,
            StartsAt = startsAt,
            MaxParticipants = maxParticipants,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string? description, string system,
        EventFormat format, string? location, string? onlineLink,
        DateTime startsAt, int maxParticipants)
    {
        Title = title;
        Description = description;
        System = system;
        Format = format;
        Location = location;
        OnlineLink = onlineLink;
        StartsAt = startsAt;
        MaxParticipants = maxParticipants;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel() { IsCancelled = true; UpdatedAt = DateTime.UtcNow; }

    public bool HasSlot => _participants.Count < MaxParticipants;
    public bool IsParticipant(UserId userId) => _participants.Any(p => p.UserId == userId);

    public void AddParticipant(UserId userId) =>
        _participants.Add(EventParticipant.Create(Id, userId));

    public void RemoveParticipant(UserId userId)
    {
        var p = _participants.FirstOrDefault(x => x.UserId == userId);
        if (p is not null) _participants.Remove(p);
    }
}
