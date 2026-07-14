using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class InitiativeTrackerRepository(AppDbContext db) : IInitiativeTrackerRepository
{
    public Task<InitiativeTracker?> GetByIdAsync(InitiativeTrackerId id, CancellationToken ct = default) =>
        db.InitiativeTrackers.FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<InitiativeTracker>> GetByCampaignAsync(CampaignId campaignId, CancellationToken ct = default) =>
        await db.InitiativeTrackers
            .Where(t => t.CampaignId == campaignId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<InitiativeTracker>> GetByLinkedSessionAsync(Guid sessionId, CancellationToken ct = default) =>
        await db.InitiativeTrackers
            .Where(t => t.LinkedSessionId == sessionId)
            .ToListAsync(ct);

    public async Task AddAsync(InitiativeTracker tracker, CancellationToken ct = default) =>
        await db.InitiativeTrackers.AddAsync(tracker, ct);

    public void Update(InitiativeTracker tracker) => db.InitiativeTrackers.Update(tracker);
    public void Delete(InitiativeTracker tracker) => db.InitiativeTrackers.Remove(tracker);
}
