using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface ICharacterRepository
{
    Task<Character?> GetByIdAsync(CharacterId id, CancellationToken ct = default);
    Task<IReadOnlyList<Character>> GetByOwnerAsync(UserId ownerId, CancellationToken ct = default);
    Task AddAsync(Character character, CancellationToken ct = default);
    void Update(Character character);
    void Delete(Character character);
}
