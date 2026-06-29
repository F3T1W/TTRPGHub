using TTRPGHub.Entities.Dnd5e;

namespace TTRPGHub.Repositories.Dnd5e;

public interface IDnd5eSpellRepository
{
    Task<(IReadOnlyList<Dnd5eSpell> Items, int Total)> SearchAsync(
        string? search, string? school, int? level, string? className,
        int page, int pageSize, CancellationToken ct = default);

    Task<Dnd5eSpell?> GetByIdAsync(Dnd5eSpellId id, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Dnd5eSpell> spells, CancellationToken ct = default);
}
