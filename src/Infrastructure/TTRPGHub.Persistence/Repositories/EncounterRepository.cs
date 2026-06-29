using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class EncounterRepository(AppDbContext db) : IEncounterRepository
{
    public Task<Encounter?> GetByIdAsync(EncounterId id, CancellationToken ct = default) =>
        db.Encounters.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<Encounter>> GetByCampaignAsync(CampaignId campaignId, CancellationToken ct = default) =>
        await db.Encounters
            .Where(e => e.CampaignId == campaignId)
            .OrderByDescending(e => e.UpdatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Encounter encounter, CancellationToken ct = default) =>
        await db.Encounters.AddAsync(encounter, ct);

    public void Update(Encounter encounter) => db.Encounters.Update(encounter);
    public void Delete(Encounter encounter) => db.Encounters.Remove(encounter);
}
