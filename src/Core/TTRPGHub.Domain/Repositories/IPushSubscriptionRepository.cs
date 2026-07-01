using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IPushSubscriptionRepository
{
    Task<IReadOnlyList<PushSubscription>> GetByUserIdAsync(UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<PushSubscription>> GetByUserIdsAsync(IReadOnlyCollection<UserId> userIds, CancellationToken ct = default);
    Task<PushSubscription?> GetByEndpointAsync(string endpoint, CancellationToken ct = default);
    Task AddAsync(PushSubscription subscription, CancellationToken ct = default);
    void Remove(PushSubscription subscription);
}
