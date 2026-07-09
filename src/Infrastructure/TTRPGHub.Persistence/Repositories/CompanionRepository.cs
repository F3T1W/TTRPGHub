using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class CompanionRepository(AppDbContext db) : ICompanionRepository
{
    public async Task<IReadOnlyList<Companion>> GetByCharacterAsync(CharacterId characterId, CancellationToken ct = default) =>
        await db.Companions
            .Where(c => c.OwnerCharacterId == characterId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public Task<Companion?> GetByIdAsync(CompanionId id, CancellationToken ct = default) =>
        db.Companions.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Companion companion, CancellationToken ct = default) =>
        await db.Companions.AddAsync(companion, ct);

    public void Update(Companion companion) => db.Companions.Update(companion);

    public void Delete(Companion companion) => db.Companions.Remove(companion);
}
