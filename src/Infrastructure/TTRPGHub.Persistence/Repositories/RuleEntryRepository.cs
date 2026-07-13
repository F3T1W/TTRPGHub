using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class RuleEntryRepository(AppDbContext db) : IRuleEntryRepository
{
    public Task<bool> AnyAsync(GameSystemId systemId, RuleCategory category, CancellationToken ct = default) =>
        db.RuleEntries.AnyAsync(e => e.SystemId == systemId && e.Category == category, ct);

    public async Task<IReadOnlyList<RuleEntry>> SearchAsync(
        GameSystemId systemId, RuleCategory category, string? search,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = Filter(systemId, category, search);
        var list = await query
            .OrderBy(e => e.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public Task<int> CountAsync(GameSystemId systemId, RuleCategory category, string? search, CancellationToken ct = default) =>
        Filter(systemId, category, search).CountAsync(ct);

    public Task<RuleEntry?> GetBySlugAsync(GameSystemId systemId, RuleCategory category, string slug, CancellationToken ct = default) =>
        db.RuleEntries.FirstOrDefaultAsync(e => e.SystemId == systemId && e.Category == category && e.Slug == slug, ct);

    public async Task<IReadOnlyList<RuleEntry>> GetBySlugsAsync(
        GameSystemId systemId, RuleCategory category, IReadOnlyCollection<string> slugs, CancellationToken ct = default)
    {
        if (slugs.Count == 0)
            return [];

        var list = await db.RuleEntries
            .Where(e => e.SystemId == systemId && e.Category == category && slugs.Contains(e.Slug))
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task AddRangeAsync(IEnumerable<RuleEntry> entries, CancellationToken ct = default) =>
        await db.RuleEntries.AddRangeAsync(entries, ct);

    public async Task AddAsync(RuleEntry entry, CancellationToken ct = default) =>
        await db.RuleEntries.AddAsync(entry, ct);

    public void Update(RuleEntry entry) =>
        db.RuleEntries.Update(entry);

    public void Remove(RuleEntry entry) =>
        db.RuleEntries.Remove(entry);

    public async Task<IReadOnlyList<RuleEntry>> GetAllBySystemAsync(GameSystemId systemId, CancellationToken ct = default)
    {
        var list = await db.RuleEntries.Where(e => e.SystemId == systemId).ToListAsync(ct);
        return list.AsReadOnly();
    }

    private IQueryable<RuleEntry> Filter(GameSystemId systemId, RuleCategory category, string? search)
    {
        var query = db.RuleEntries.Where(e => e.SystemId == systemId && e.Category == category);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(e => EF.Functions.ILike(e.Title, $"%{search}%"));
        return query;
    }
}
