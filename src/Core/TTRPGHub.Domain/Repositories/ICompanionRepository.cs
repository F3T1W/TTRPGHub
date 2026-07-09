using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ICompanionRepository
{
    Task<IReadOnlyList<Companion>> GetByCharacterAsync(CharacterId characterId, CancellationToken ct = default);
    Task<Companion?> GetByIdAsync(CompanionId id, CancellationToken ct = default);
    Task AddAsync(Companion companion, CancellationToken ct = default);
    void Update(Companion companion);
    void Delete(Companion companion);
}
