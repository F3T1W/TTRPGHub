using TTRPGHub.Entities.Moderation;

namespace TTRPGHub.Repositories;

public interface IContentReportRepository
{
    Task<ContentReport?> GetByIdAsync(ContentReportId id, CancellationToken ct = default);
    Task<IReadOnlyList<ContentReport>> GetOpenAsync(CancellationToken ct = default);
    Task AddAsync(ContentReport report, CancellationToken ct = default);
    void Update(ContentReport report);
}
