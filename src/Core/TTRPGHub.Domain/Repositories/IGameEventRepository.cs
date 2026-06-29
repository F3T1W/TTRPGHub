using TTRPGHub.Entities;
using TTRPGHub.Entities.Events;

namespace TTRPGHub.Repositories;

public interface IGameEventRepository
{
    Task<List<GameEvent>> GetUpcomingAsync(int page, int pageSize, CancellationToken ct = default);
    Task<int> CountUpcomingAsync(CancellationToken ct = default);
    Task<GameEvent?> GetByIdWithParticipantsAsync(GameEventId id, CancellationToken ct = default);
    Task<List<GameEvent>> GetByOrganizerAsync(UserId organizerId, CancellationToken ct = default);
    Task AddAsync(GameEvent gameEvent, CancellationToken ct = default);
    void Remove(GameEvent gameEvent);
}
