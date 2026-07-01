using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class GameSystemRepository(AppDbContext db) : IGameSystemRepository
{
    public Task<GameSystem?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        db.GameSystems.FirstOrDefaultAsync(s => s.Slug == slug, ct);

    public Task<GameSystem?> GetByIdAsync(GameSystemId id, CancellationToken ct = default) =>
        db.GameSystems.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<GameSystem>> GetAllAsync(CancellationToken ct = default) =>
        await db.GameSystems.OrderBy(s => s.Name).ToListAsync(ct);

    public Task<bool> ExistsAsync(string slug, CancellationToken ct = default) =>
        db.GameSystems.AnyAsync(s => s.Slug == slug, ct);

    public async Task AddAsync(GameSystem system, CancellationToken ct = default) =>
        await db.GameSystems.AddAsync(system, ct);
}
