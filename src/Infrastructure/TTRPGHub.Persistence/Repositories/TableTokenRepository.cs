using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class TableTokenRepository(AppDbContext db) : ITableTokenRepository
{
    public async Task<IReadOnlyList<TableToken>> GetBySessionAsync(GameSessionId sessionId, CancellationToken ct = default)
    {
        var list = await db.TableTokens
            .Include(t => t.Conditions)
            .Where(t => t.SessionId == sessionId)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<IReadOnlyList<TableToken>> GetBySceneAsync(Guid sceneId, CancellationToken ct = default)
    {
        var list = await db.TableTokens
            .Include(t => t.Conditions)
            .Where(t => t.SceneId == sceneId)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public Task<TableToken?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.TableTokens.Include(t => t.Conditions).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<IReadOnlyList<TableToken>> GetByCombatantAsync(TokenCombatantType combatantType, Guid combatantId, CancellationToken ct = default)
    {
        var list = await db.TableTokens
            .Include(t => t.Conditions)
            .Where(t => t.CombatantType == combatantType && t.CombatantId == combatantId)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task AddAsync(TableToken token, CancellationToken ct = default) =>
        await db.TableTokens.AddAsync(token, ct);

    public void Update(TableToken token) =>
        db.TableTokens.Update(token);

    public void Remove(TableToken token) =>
        db.TableTokens.Remove(token);
}
