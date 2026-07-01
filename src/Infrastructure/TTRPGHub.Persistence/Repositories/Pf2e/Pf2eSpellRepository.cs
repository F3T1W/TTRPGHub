using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

internal sealed class Pf2eSpellRepository(AppDbContext db) : IPf2eSpellRepository
{
    public async Task<(IReadOnlyList<Pf2eSpell> Items, int Total)> SearchAsync(
        string? search, string? tradition, int? level, string? trait,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Pf2eSpells.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(tradition))
            q = q.Where(s => s.Traditions.ToLower().Contains(tradition.ToLower()));

        if (level.HasValue)
            q = q.Where(s => s.Level == level.Value);

        if (!string.IsNullOrWhiteSpace(trait))
            q = q.Where(s => s.Traits.ToLower().Contains(trait.ToLower()));

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(s => s.Level).ThenBy(s => s.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public Task<Pf2eSpell?> GetByIdAsync(Pf2eSpellId id, CancellationToken ct = default) =>
        db.Pf2eSpells.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        db.Pf2eSpells.AnyAsync(ct);

    public async Task AddRangeAsync(IEnumerable<Pf2eSpell> spells, CancellationToken ct = default) =>
        await db.Pf2eSpells.AddRangeAsync(spells, ct);
}
