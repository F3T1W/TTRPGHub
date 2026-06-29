using TTRPGHub.Common;
using TTRPGHub.Events;

namespace TTRPGHub.Entities;

public enum CampaignStatus { Active, Paused, Completed, Archived }

public sealed class Campaign : Entity<CampaignId>
{
    private readonly List<CampaignParticipant> _participants = [];

    public new CampaignId Id { get; private set; }
    public UserId OrganizerId { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public string System { get; private set; } = "";
    public CampaignStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<CampaignParticipant> Participants => _participants.AsReadOnly();

    private Campaign() { }

    public static Campaign Create(UserId organizerId, string title, string? description, string system)
    {
        var campaign = new Campaign
        {
            Id           = CampaignId.New(),
            OrganizerId  = organizerId,
            Title        = title,
            Description  = description,
            System       = system,
            Status       = CampaignStatus.Active,
            CreatedAt    = DateTime.UtcNow,
            UpdatedAt    = DateTime.UtcNow,
        };
        campaign._participants.Add(CampaignParticipant.Create(organizerId, campaign.Id, CampaignRole.DungeonMaster));
        campaign.RaiseDomainEvent(new CampaignCreatedEvent(campaign.Id, organizerId));
        return campaign;
    }

    public Result Update(string title, string? description, string system)
    {
        Title       = title;
        Description = description;
        System      = system;
        UpdatedAt   = DateTime.UtcNow;
        return Result.Success();
    }

    public Result AddParticipant(UserId userId)
    {
        if (_participants.Any(p => p.UserId == userId))
            return Error.Conflict("CampaignParticipant");

        _participants.Add(CampaignParticipant.Create(userId, Id, CampaignRole.Player));
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RemoveParticipant(UserId userId)
    {
        if (userId == OrganizerId)
            return Error.Validation("Campaign", "Организатор не может покинуть кампанию.");

        var participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
            return Error.NotFound("CampaignParticipant");

        _participants.Remove(participant);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result ChangeStatus(CampaignStatus status)
    {
        Status    = status;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
