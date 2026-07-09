using TTRPGHub.Entities.Moderation;

namespace TTRPGHub.Repositories;

public interface IModerationLogRepository
{
    Task<IReadOnlyList<ModerationLogEntry>> GetRecentAsync(int take = 200, CancellationToken ct = default);
    Task AddAsync(ModerationLogEntry entry, CancellationToken ct = default);
}
