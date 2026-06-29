using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IInitiativeTrackerRepository
{
    Task<InitiativeTracker?> GetByIdAsync(InitiativeTrackerId id, CancellationToken ct = default);
    Task<IReadOnlyList<InitiativeTracker>> GetByCampaignAsync(CampaignId campaignId, CancellationToken ct = default);
    Task AddAsync(InitiativeTracker tracker, CancellationToken ct = default);
    void Update(InitiativeTracker tracker);
    void Delete(InitiativeTracker tracker);
}
