using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ISceneRepository
{
    Task<IReadOnlyList<Scene>> GetBySessionAsync(GameSessionId sessionId, CancellationToken ct = default);
    Task<Scene?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Scene scene, CancellationToken ct = default);
    void Update(Scene scene);
    void Remove(Scene scene);
}
