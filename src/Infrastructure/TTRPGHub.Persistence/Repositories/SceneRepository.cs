using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class SceneRepository(AppDbContext db) : ISceneRepository
{
    public async Task<IReadOnlyList<Scene>> GetBySessionAsync(GameSessionId sessionId, CancellationToken ct = default)
    {
        var list = await db.Scenes
            .Where(s => s.SessionId == sessionId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public Task<Scene?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Scenes.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task AddAsync(Scene scene, CancellationToken ct = default) =>
        await db.Scenes.AddAsync(scene, ct);

    public void Update(Scene scene) => db.Scenes.Update(scene);

    public void Remove(Scene scene) => db.Scenes.Remove(scene);
}
