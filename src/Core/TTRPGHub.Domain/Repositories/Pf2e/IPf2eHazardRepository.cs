using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

public interface IPf2eHazardRepository
{
    Task<(IReadOnlyList<Pf2eHazard> Items, int Total)> SearchAsync(
        string? search, int? level, int page, int pageSize, CancellationToken ct = default);

    Task<Pf2eHazard?> GetByIdAsync(Pf2eHazardId id, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Pf2eHazard> hazards, CancellationToken ct = default);
}
