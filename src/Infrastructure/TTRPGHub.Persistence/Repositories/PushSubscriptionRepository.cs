using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class PushSubscriptionRepository(AppDbContext db) : IPushSubscriptionRepository
{
    public async Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(UserId userId, CancellationToken ct = default) =>
        await db.PushSubscriptions.Where(p => p.UserId == userId).ToListAsync(ct);

    public async Task<IReadOnlyList<PushSubscription>> GetByUserIdsAsync(IReadOnlyCollection<UserId> userIds, CancellationToken ct = default) =>
        await db.PushSubscriptions.Where(p => userIds.Contains(p.UserId)).ToListAsync(ct);

    public Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct = default) =>
        db.PushSubscriptions.FirstOrDefaultAsync(p => p.Endpoint == endpoint, ct);

    public async Task AddAsync(PushSubscription subscription, CancellationToken ct = default) =>
        await db.PushSubscriptions.AddAsync(subscription, ct);

    public void Remove(PushSubscription subscription) =>
        db.PushSubscriptions.Remove(subscription);
}
