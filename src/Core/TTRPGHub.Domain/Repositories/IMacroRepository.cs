using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IMacroRepository
{
    Task<IReadOnlyList<Macro>> GetByOwnerAsync(UserId ownerId, CancellationToken ct = default);
    Task<Macro?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Macro>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
    Task AddAsync(Macro macro, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<Macro> macros, CancellationToken ct = default);
    void Update(Macro macro);
    void Remove(Macro macro);
}
