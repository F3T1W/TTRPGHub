using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IEncounterRepository
{
    Task<Encounter?> GetByIdAsync(EncounterId id, CancellationToken ct = default);
    Task<IReadOnlyList<Encounter>> GetByCampaignAsync(CampaignId campaignId, CancellationToken ct = default);
    Task AddAsync(Encounter encounter, CancellationToken ct = default);
    void Update(Encounter encounter);
    void Delete(Encounter encounter);
}
