using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Events;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class GameEventRepository(AppDbContext db) : IGameEventRepository
{
    public Task<List<GameEvent>> GetUpcomingAsync(
        int page, int pageSize, string? location = null, EventFormat? format = null, CancellationToken ct = default) =>
        Filter(location, format)
            .Include(e => e.Organizer)
            .Include(e => e.Participants)
            .OrderBy(e => e.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountUpcomingAsync(string? location = null, EventFormat? format = null, CancellationToken ct = default) =>
        Filter(location, format).CountAsync(ct);

    private IQueryable<GameEvent> Filter(string? location, EventFormat? format)
    {
        var query = db.GameEvents.Where(e => !e.IsCancelled && e.StartsAt >= DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(e => e.Location != null && EF.Functions.ILike(e.Location, $"%{location}%"));
        if (format.HasValue)
            query = query.Where(e => e.Format == format.Value);

        return query;
    }

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
