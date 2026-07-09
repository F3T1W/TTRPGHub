using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

internal sealed class PathfinderSocietyChronicleRepository(AppDbContext db) : IPathfinderSocietyChronicleRepository
{
    public async Task<IReadOnlyList<PathfinderSocietyChronicle>> GetByCharacterAsync(
        CharacterId characterId, CancellationToken ct = default) =>
        await db.PathfinderSocietyChronicles
            .Where(c => c.CharacterId == characterId)
            .OrderByDescending(c => c.SessionDate)
            .ToListAsync(ct);

    public Task<PathfinderSocietyChronicle?> GetByIdAsync(PathfinderSocietyChronicleId id, CancellationToken ct = default) =>
        db.PathfinderSocietyChronicles.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(PathfinderSocietyChronicle chronicle, CancellationToken ct = default) =>
        await db.PathfinderSocietyChronicles.AddAsync(chronicle, ct);

    public void Delete(PathfinderSocietyChronicle chronicle) =>
        db.PathfinderSocietyChronicles.Remove(chronicle);
}
