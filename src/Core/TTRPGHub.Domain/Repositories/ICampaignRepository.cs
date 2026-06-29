using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ICampaignRepository
{
    Task<Campaign?> GetByIdAsync(CampaignId id, CancellationToken ct = default);
    Task<IReadOnlyList<Campaign>> GetByOrganizerAsync(UserId organizerId, CancellationToken ct = default);
    Task<IReadOnlyList<Campaign>> GetByParticipantAsync(UserId userId, CancellationToken ct = default);
    Task AddAsync(Campaign campaign, CancellationToken ct = default);
    void Update(Campaign campaign);
}
