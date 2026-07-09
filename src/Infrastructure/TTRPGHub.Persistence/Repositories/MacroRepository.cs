using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class MacroRepository(AppDbContext db) : IMacroRepository
{
    public async Task<IReadOnlyList<Macro>> GetByOwnerAsync(UserId ownerId, CancellationToken ct = default)
    {
        var list = await db.Macros
            .Where(m => m.OwnerId == ownerId)
            .OrderBy(m => m.Name)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public Task<Macro?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Macros.FirstOrDefaultAsync(m => m.Id == id, ct);

    public async Task AddAsync(Macro macro, CancellationToken ct = default) =>
        await db.Macros.AddAsync(macro, ct);

    public async Task AddRangeAsync(IEnumerable<Macro> macros, CancellationToken ct = default) =>
        await db.Macros.AddRangeAsync(macros, ct);

    public void Update(Macro macro) => db.Macros.Update(macro);

    public void Remove(Macro macro) => db.Macros.Remove(macro);
}
