using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class CampaignRepository(AppDbContext db) : ICampaignRepository
{
    public Task<Campaign?> GetByIdAsync(CampaignId id, CancellationToken ct = default) =>
        db.Campaigns.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Campaign>> GetByOrganizerAsync(UserId organizerId, CancellationToken ct = default) =>
        await db.Campaigns
            .Where(c => c.OrganizerId == organizerId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Campaign>> GetByParticipantAsync(UserId userId, CancellationToken ct = default) =>
        await db.Campaigns
            .Where(c => c.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Campaign campaign, CancellationToken ct = default) =>
        await db.Campaigns.AddAsync(campaign, ct);

    public void Update(Campaign campaign) =>
        db.Campaigns.Update(campaign);
}
