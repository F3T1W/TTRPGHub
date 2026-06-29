using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Events;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class GameEventRepository(AppDbContext db) : IGameEventRepository
{
    public Task<List<GameEvent>> GetUpcomingAsync(int page, int pageSize, CancellationToken ct) =>
        db.GameEvents
            .Include(e => e.Organizer)
            .Include(e => e.Participants)
            .Where(e => !e.IsCancelled && e.StartsAt >= DateTime.UtcNow)
            .OrderBy(e => e.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountUpcomingAsync(CancellationToken ct) =>
        db.GameEvents.CountAsync(e => !e.IsCancelled && e.StartsAt >= DateTime.UtcNow, ct);

    public Task<GameEvent?> GetByIdWithParticipantsAsync(GameEventId id, CancellationToken ct) =>
        db.GameEvents
            .Include(e => e.Organizer)
            .Include(e => e.Participants).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<List<GameEvent>> GetByOrganizerAsync(UserId organizerId, CancellationToken ct) =>
        db.GameEvents
            .Include(e => e.Participants)
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.StartsAt)
            .ToListAsync(ct);

    public async Task AddAsync(GameEvent gameEvent, CancellationToken ct) =>
        await db.GameEvents.AddAsync(gameEvent, ct);

    public void Remove(GameEvent gameEvent) =>
        db.GameEvents.Remove(gameEvent);
}
