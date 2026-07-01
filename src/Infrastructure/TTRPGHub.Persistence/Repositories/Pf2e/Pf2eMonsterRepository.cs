using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

internal sealed class Pf2eMonsterRepository(AppDbContext db) : IPf2eMonsterRepository
{
    public async Task<(IReadOnlyList<Pf2eMonster> Items, int Total)> SearchAsync(
        string? search, string? trait, string? size, int? level,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Pf2eMonsters.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(m => m.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(trait))
            q = q.Where(m => m.Traits.ToLower().Contains(trait.ToLower()));

        if (!string.IsNullOrWhiteSpace(size))
            q = q.Where(m => m.Size.ToLower() == size.ToLower());

        if (level.HasValue)
            q = q.Where(m => m.Level == level.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(m => m.Level).ThenBy(m => m.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public Task<Pf2eMonster?> GetByIdAsync(Pf2eMonsterId id, CancellationToken ct = default) =>
        db.Pf2eMonsters.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        db.Pf2eMonsters.AnyAsync(ct);

    public async Task AddRangeAsync(IEnumerable<Pf2eMonster> monsters, CancellationToken ct = default) =>
        await db.Pf2eMonsters.AddRangeAsync(monsters, ct);
}
