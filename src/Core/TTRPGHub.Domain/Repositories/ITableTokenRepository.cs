using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ITableTokenRepository
{
    Task<IReadOnlyList<TableToken>> GetBySessionAsync(GameSessionId sessionId, CancellationToken ct = default);
    Task<TableToken?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(TableToken token, CancellationToken ct = default);
    void Update(TableToken token);
    void Remove(TableToken token);
}
