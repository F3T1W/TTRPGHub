using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ITableMessageRepository
{
    Task<IReadOnlyList<TableMessage>> GetRecentAsync(GameSessionId sessionId, int take = 100, CancellationToken ct = default);
    Task AddAsync(TableMessage message, CancellationToken ct = default);
}
