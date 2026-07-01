using Microsoft.EntityFrameworkCore;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;

namespace TTRPGHub.Persistence.Repositories;

internal sealed class ContentReportRepository(AppDbContext db) : IContentReportRepository
{
    public Task<ContentReport?> GetByIdAsync(ContentReportId id, CancellationToken ct = default) =>
        db.ContentReports.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<ContentReport>> GetOpenAsync(CancellationToken ct = default) =>
        await db.ContentReports
            .Where(r => r.Status == ReportStatus.Open)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(ContentReport report, CancellationToken ct = default) =>
        await db.ContentReports.AddAsync(report, ct);

    public void Update(ContentReport report) =>
        db.ContentReports.Update(report);
}
