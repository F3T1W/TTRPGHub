using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ISessionReminderLogRepository
{
    Task<bool> ExistsAsync(GameSessionId sessionId, UserId userId, CancellationToken ct = default);
    Task<HashSet<UserId>> GetNotifiedUserIdsAsync(GameSessionId sessionId, CancellationToken ct = default);
    Task AddAsync(SessionReminderLog log, CancellationToken ct = default);
}
