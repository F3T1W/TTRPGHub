using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Repositories.Pf2e;

public interface IPathfinderSocietyChronicleRepository
{
    Task<IReadOnlyList<PathfinderSocietyChronicle>> GetByCharacterAsync(CharacterId characterId, CancellationToken ct = default);
    Task<PathfinderSocietyChronicle?> GetByIdAsync(PathfinderSocietyChronicleId id, CancellationToken ct = default);
    Task AddAsync(PathfinderSocietyChronicle chronicle, CancellationToken ct = default);
    void Delete(PathfinderSocietyChronicle chronicle);
}
