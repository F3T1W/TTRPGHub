using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class TableMessageRepository(AppDbContext db) : ITableMessageRepository
{
    public async Task<IReadOnlyList<TableMessage>> GetRecentAsync(GameSessionId sessionId, int take = 100, CancellationToken ct = default)
    {
        var list = await db.TableMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task AddAsync(TableMessage message, CancellationToken ct = default) =>
        await db.TableMessages.AddAsync(message, ct);
}
