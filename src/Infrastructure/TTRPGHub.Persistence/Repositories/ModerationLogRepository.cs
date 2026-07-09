using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class ModerationLogRepository(AppDbContext db) : IModerationLogRepository
{
    public async Task<IReadOnlyList<ModerationLogEntry>> GetRecentAsync(int take = 200, CancellationToken ct = default) =>
        await db.ModerationLogEntries
            .OrderByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(ModerationLogEntry entry, CancellationToken ct = default) =>
        await db.ModerationLogEntries.AddAsync(entry, ct);
}
