using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

public interface IPf2eMonsterRepository
{
    Task<(IReadOnlyList<Pf2eMonster> Items, int Total)> SearchAsync(
        string? search, string? trait, string? size, int? level,
        int page, int pageSize, CancellationToken ct = default);

    Task<Pf2eMonster?> GetByIdAsync(Pf2eMonsterId id, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Pf2eMonster> monsters, CancellationToken ct = default);
}
