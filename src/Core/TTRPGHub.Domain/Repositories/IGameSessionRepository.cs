using TTRPGHub.Entities;

namespace TTRPGHub.Repositories;

public interface IGameSessionRepository
{
    Task<GameSession?> GetByIdAsync(GameSessionId id, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetUpcomingAsync(
        int page, int pageSize, string? location = null, SessionFormat? format = null, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetByOrganizerAsync(UserId organizerId, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetByParticipantAsync(UserId userId, CancellationToken ct = default);
    Task<IReadOnlyList<GameSession>> GetScheduledBetweenAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task AddAsync(GameSession session, CancellationToken ct = default);
    void Update(GameSession session);
}
