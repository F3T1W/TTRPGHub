using TTRPGHub.Entities.Dnd5e;

namespace TTRPGHub.Repositories.Dnd5e;

public interface IDnd5eMonsterRepository
{
    Task<(IReadOnlyList<Dnd5eMonster> Items, int Total)> SearchAsync(
        string? search, string? type, string? size, string? cr,
        int page, int pageSize, CancellationToken ct = default);

    Task<Dnd5eMonster?> GetByIdAsync(Dnd5eMonsterId id, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Dnd5eMonster> monsters, CancellationToken ct = default);
}
