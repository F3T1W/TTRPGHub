using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

internal sealed class GameSessionRepository(AppDbContext db) : IGameSessionRepository
{
    public Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken ct = default) =>
        db.GameSessions
            .Include(s => s.Participants)
            .FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<GameSession>> GetUpcomingAsync(
        int page, int pageSize, string? location = null, SessionFormat? format = null, CancellationToken ct = default)
    {
        var query = db.GameSessions
            .Include(s => s.Participants)
            .Where(s => s.Status == SessionStatus.Planned && s.ScheduledAt > DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(s => s.Location != null && EF.Functions.ILike(s.Location, $"%{location}%"));
        if (format.HasValue)
            query = query.Where(s => s.Format == format.Value);

        var list = await query
            .OrderBy(s => s.ScheduledAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<IReadOnlyList<GameSession>> GetByOrganizerAsync(UserId organizerId, CancellationToken ct = default)
    {
        var list = await db.GameSessions
            .Include(s => s.Participants)
            .Where(s => s.OrganizerId == organizerId)
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<IReadOnlyList<GameSession>> GetByParticipantAsync(UserId userId, CancellationToken ct = default)
    {
        var list = await db.GameSessions
            .Include(s => s.Participants)
            .Where(s => s.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(s => s.ScheduledAt)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task<IReadOnlyList<GameSession>> GetScheduledBetweenAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var list = await db.GameSessions
            .Include(s => s.Participants)
            .Where(s => s.Status == SessionStatus.Planned && s.ScheduledAt >= from && s.ScheduledAt <= to)
            .ToListAsync(ct);
        return list.AsReadOnly();
    }

    public async Task AddAsync(GameSession session, CancellationToken ct = default) =>
        await db.GameSessions.AddAsync(session, ct);

    public void Update(GameSession session) =>
        db.GameSessions.Update(session);
}
