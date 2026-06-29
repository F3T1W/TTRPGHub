namespace TTRPGHub.Entities;

public enum CampaignRole { DungeonMaster, Player }

public sealed class CampaignParticipant
{
    public UserId UserId { get; private set; }
    public CampaignId CampaignId { get; private set; }
    public CampaignRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }

    private CampaignParticipant() { }

    public static CampaignParticipant Create(UserId userId, CampaignId campaignId, CampaignRole role) => new()
    {
        UserId     = userId,
        CampaignId = campaignId,
        Role       = role,
        JoinedAt   = DateTime.UtcNow
    };
}
