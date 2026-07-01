using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IGameSystemRepository
{
    Task<GameSystem?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<GameSystem?> GetByIdAsync(GameSystemId id, CancellationToken ct = default);
    Task<IReadOnlyList<GameSystem>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(string slug, CancellationToken ct = default);
    Task AddAsync(GameSystem system, CancellationToken ct = default);
}
