using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

internal sealed class Pf2eHazardRepository(AppDbContext db) : IPf2eHazardRepository
{
    public async Task<(IReadOnlyList<Pf2eHazard> Items, int Total)> SearchAsync(
        string? search, int? level, int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Pf2eHazards.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(h => h.NameRu.ToLower().Contains(search.ToLower()) || h.Name.ToLower().Contains(search.ToLower()));

        if (level.HasValue)
            q = q.Where(h => h.Level == level.Value);

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(h => h.Level).ThenBy(h => h.NameRu)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return (items, total);
    }

    public Task<Pf2eHazard?> GetByIdAsync(Pf2eHazardId id, CancellationToken ct = default) =>
        db.Pf2eHazards.FirstOrDefaultAsync(h => h.Id == id, ct);

    public Task<bool> AnyAsync(CancellationToken ct = default) =>
        db.Pf2eHazards.AnyAsync(ct);

    public async Task AddRangeAsync(IEnumerable<Pf2eHazard> hazards, CancellationToken ct = default) =>
        await db.Pf2eHazards.AddRangeAsync(hazards, ct);
}
