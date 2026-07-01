using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Repositories;

internal sealed class SessionReminderLogRepository(AppDbContext db) : ISessionReminderLogRepository
{
    public Task<bool> ExistsAsync(GameSessionId sessionId, UserId userId, CancellationToken ct = default) =>
        db.SessionReminderLogs.AnyAsync(r => r.SessionId == sessionId && r.UserId == userId, ct);

    public async Task<HashSet<UserId>> GetNotifiedUserIdsAsync(GameSessionId sessionId, CancellationToken ct = default) =>
        (await db.SessionReminderLogs
            .Where(r => r.SessionId == sessionId)
            .Select(r => r.UserId)
            .ToListAsync(ct))
        .ToHashSet();

    public async Task AddAsync(SessionReminderLog log, CancellationToken ct = default) =>
        await db.SessionReminderLogs.AddAsync(log, ct);
}
