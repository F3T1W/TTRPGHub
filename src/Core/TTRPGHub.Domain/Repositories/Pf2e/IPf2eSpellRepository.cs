using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

public interface IPf2eSpellRepository
{
    Task<(IReadOnlyList<Pf2eSpell> Items, int Total)> SearchAsync(
        string? search, string? tradition, int? level, string? trait,
        int page, int pageSize, CancellationToken ct = default);

    Task<Pf2eSpell?> GetByIdAsync(Pf2eSpellId id, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Pf2eSpell> spells, CancellationToken ct = default);
    Task<IReadOnlyList<Pf2eSpell>> GetAllForAutomationSyncAsync(CancellationToken ct = default);
}
