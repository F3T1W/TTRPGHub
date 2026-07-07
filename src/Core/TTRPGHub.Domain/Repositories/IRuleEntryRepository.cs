using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IRuleEntryRepository
{
    Task<bool> AnyAsync(GameSystemId systemId, RuleCategory category, CancellationToken ct = default);

    Task<IReadOnlyList<RuleEntry>> SearchAsync(
        GameSystemId systemId, RuleCategory category, string? search,
        int page, int pageSize, CancellationToken ct = default);

    Task<int> CountAsync(GameSystemId systemId, RuleCategory category, string? search, CancellationToken ct = default);

    Task<RuleEntry?> GetBySlugAsync(GameSystemId systemId, RuleCategory category, string slug, CancellationToken ct = default);

    Task<IReadOnlyList<RuleEntry>> GetBySlugsAsync(
        GameSystemId systemId, RuleCategory category, IReadOnlyCollection<string> slugs, CancellationToken ct = default);

    Task AddRangeAsync(IEnumerable<RuleEntry> entries, CancellationToken ct = default);
    Task AddAsync(RuleEntry entry, CancellationToken ct = default);
    void Update(RuleEntry entry);
    void Remove(RuleEntry entry);
}
