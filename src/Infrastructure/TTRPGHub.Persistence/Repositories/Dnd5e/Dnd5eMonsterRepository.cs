using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Repositories.Dnd5e;

namespace TTRPGHub.Repositories.Dnd5e;

internal sealed class Dnd5eMonsterRepository(AppDbContext db) : IDnd5eMonsterRepository
{
    public async Task<(IReadOnlyList<Dnd5eMonster> Items, int Total)> SearchAsync(
        string? search, string? type, string? size, string? cr,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Dnd5eMonsters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(m => m.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(type))
            q = q.Where(m => m.Type.ToLower() == type.ToLower());

        if (!string.IsNullOrWhiteSpace(size))
            q = q.Where(m => m.Size.ToLower() == size.ToLower());

        if (!string.IsNullOrWhiteSpace(cr))
            q = q.Where(m => m.ChallengeRating == cr);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(m => m.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public Task<Dnd5eMonster?> GetByIdAsync(Dnd5eMonsterId id, CancellationToken ct = default) =>
        db.Dnd5eMonsters.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        db.Dnd5eMonsters.AnyAsync(ct);

    public async Task AddRangeAsync(IEnumerable<Dnd5eMonster> monsters, CancellationToken ct = default) =>
        await db.Dnd5eMonsters.AddRangeAsync(monsters, ct);
}
