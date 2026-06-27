using Microsoft.EntityFrameworkCore;
using TTRPGHub.Domain.Entities;
using TTRPGHub.Domain.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class CharacterRepository(AppDbContext db) : ICharacterRepository
{
    public Task<Character?> GetByIdAsync(CharacterId id, CancellationToken ct = default) =>
        db.Characters.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Character>> GetByOwnerAsync(UserId ownerId, CancellationToken ct = default)
    {
        var list = await db.Characters
            .Where(c => c.OwnerId == ownerId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task AddAsync(Character character, CancellationToken ct = default) =>
        await db.Characters.AddAsync(character, ct);

    public void Update(Character character) =>
        db.Characters.Update(character);

    public void Delete(Character character) =>
        db.Characters.Remove(character);
}
