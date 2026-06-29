using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Repositories.Dnd5e;

namespace TTRPGHub.Repositories.Dnd5e;

internal sealed class Dnd5eSpellRepository(AppDbContext db) : IDnd5eSpellRepository
{
    public async Task<(IReadOnlyList<Dnd5eSpell> Items, int Total)> SearchAsync(
        string? search, string? school, int? level, string? className,
        int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Dnd5eSpells.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(s => s.Name.ToLower().Contains(search.ToLower()));

        if (!string.IsNullOrWhiteSpace(school))
            q = q.Where(s => s.School.ToLower() == school.ToLower());

        if (level.HasValue)
            q = q.Where(s => s.Level == level.Value);

        if (!string.IsNullOrWhiteSpace(className))
            q = q.Where(s => s.Classes.ToLower().Contains(className.ToLower()));

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(s => s.Level).ThenBy(s => s.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public Task<Dnd5eSpell?> GetByIdAsync(Dnd5eSpellId id, CancellationToken ct = default) =>
        db.Dnd5eSpells.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        db.Dnd5eSpells.AnyAsync(ct);

    public async Task AddRangeAsync(IEnumerable<Dnd5eSpell> spells, CancellationToken ct = default) =>
        await db.Dnd5eSpells.AddRangeAsync(spells, ct);
}
