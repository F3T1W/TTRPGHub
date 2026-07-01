using TTRPGHub.Common;
using TTRPGHub.Events;

namespace TTRPGHub.Entities;

public sealed class GameSession : Entity<GameSessionId>
{
    public UserId OrganizerId { get; private init; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public string System { get; private set; } = null!;   // «D&D 5e», «Pathfinder», etc.
    public int MaxPlayers { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public SessionFormat Format { get; private set; }
    public string? Location { get; private set; }
    public SessionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }
    public string? CurrentShowcaseImageUrl { get; private set; }
    public string? CurrentTrackUrl { get; private set; }
    public string? CurrentTrackTitle { get; private set; }
    public bool IsAudioPlaying { get; private set; }
    public double AudioPositionSeconds { get; private set; }
    public DateTime AudioUpdatedAt { get; private set; }

    private readonly List<SessionParticipant> _participants = [];
    public IReadOnlyList<SessionParticipant> Participants => _participants.AsReadOnly();

    private GameSession() { }

    public static GameSession Create(
        UserId organizerId, string title, string? description,
        string system, int maxPlayers, DateTime scheduledAt,
        SessionFormat format, string? location)
    {
        var now = DateTime.UtcNow;
        var session = new GameSession
        {
            Id = GameSessionId.New(),
            OrganizerId = organizerId,
            Title = title,
            Description = description,
            System = system,
            MaxPlayers = maxPlayers,
            ScheduledAt = scheduledAt,
            Format = format,
            Location = location,
            Status = SessionStatus.Planned,
            CreatedAt = now,
            UpdatedAt = now
        };
        session._participants.Add(SessionParticipant.Create(organizerId, session.Id, ParticipantRole.DungeonMaster));
        session.RaiseDomainEvent(new GameSessionCreatedEvent(session.Id, organizerId));
        return session;
    }

    public Error? Join(UserId userId)
    {
        if (Status != SessionStatus.Planned)
            return Error.Validation("Session.NotOpen", "Сессия не принимает новых участников.");
        if (_participants.Any(p => p.UserId == userId))
            return Error.Validation("Session.AlreadyJoined", "Вы уже участвуете в этой сессии.");
        if (_participants.Count(p => p.Role == ParticipantRole.Player) >= MaxPlayers - 1)
            return Error.Validation("Session.Full", "Мест в сессии нет.");

        _participants.Add(SessionParticipant.Create(userId, Id, ParticipantRole.Player));
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? Leave(UserId userId)
    {
        if (OrganizerId == userId)
            return Error.Validation("Session.OrganizerCannotLeave", "Организатор не может покинуть сессию.");
        var p = _participants.FirstOrDefault(x => x.UserId == userId);
        if (p is null)
            return Error.Validation("Session.NotParticipant", "Вы не являетесь участником.");

        _participants.Remove(p);
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? Start(UserId requesterId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();
        if (Status != SessionStatus.Planned)
            return Error.Validation("Session.WrongStatus", "Сессию нельзя начать в текущем статусе.");

        Status = SessionStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? Complete(UserId requesterId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();
        if (Status != SessionStatus.InProgress)
            return Error.Validation("Session.WrongStatus", "Сессию нельзя завершить в текущем статусе.");

        Status = SessionStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? Cancel(UserId requesterId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();
        if (Status == SessionStatus.Completed || Status == SessionStatus.Cancelled)
            return Error.Validation("Session.WrongStatus", "Сессию нельзя отменить в текущем статусе.");

        Status = SessionStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public void Update(
        string title, string? description, string system, int maxPlayers, DateTime scheduledAt,
        SessionFormat format, string? location)
    {
        Title = title;
        Description = description;
        System = system;
        MaxPlayers = maxPlayers;
        ScheduledAt = scheduledAt;
        Format = format;
        Location = location;
        UpdatedAt = DateTime.UtcNow;
    }

    public Error? SetShowcaseImage(UserId requesterId, string? imageUrl)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        CurrentShowcaseImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? SetTrack(UserId requesterId, string trackUrl, string? trackTitle)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        CurrentTrackUrl = trackUrl;
        CurrentTrackTitle = trackTitle;
        IsAudioPlaying = false;
        AudioPositionSeconds = 0;
        AudioUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? PlayAudio(UserId requesterId, double positionSeconds)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();
        if (CurrentTrackUrl is null)
            return Error.Validation("Audio.NoTrack", "Трек не выбран.");

        IsAudioPlaying = true;
        AudioPositionSeconds = positionSeconds;
        AudioUpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? PauseAudio(UserId requesterId, double positionSeconds)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        IsAudioPlaying = false;
        AudioPositionSeconds = positionSeconds;
        AudioUpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? SeekAudio(UserId requesterId, double positionSeconds)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        AudioPositionSeconds = positionSeconds;
        AudioUpdatedAt = DateTime.UtcNow;
        return null;
    }

    public Error? ClearAudio(UserId requesterId)
    {
        if (OrganizerId != requesterId)
            return Error.Unauthorized();

        CurrentTrackUrl = null;
        CurrentTrackTitle = null;
        IsAudioPlaying = false;
        AudioPositionSeconds = 0;
        AudioUpdatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        return null;
    }

    public bool IsParticipant(UserId userId) => _participants.Any(p => p.UserId == userId);
}

public enum SessionStatus { Planned, InProgress, Completed, Cancelled }
public enum SessionFormat { Online, Offline, Hybrid }
