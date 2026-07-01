using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Moderation.Queries.GetOpenReports;

internal sealed class GetOpenReportsQueryHandler(
    IContentReportRepository reports,
    IUserRepository users
) : IRequestHandler<GetOpenReportsQuery, Result<List<ContentReportDto>>>
{
    public async Task<Result<List<ContentReportDto>>> Handle(GetOpenReportsQuery request, CancellationToken ct)
    {
        var openReports = await reports.GetOpenAsync(ct);
        var result = new List<ContentReportDto>(openReports.Count);

        foreach (var r in openReports)
        {
            var reporter = await users.GetByIdAsync(r.ReporterId, ct);
            result.Add(new ContentReportDto(
                r.Id.Value, r.EntityType.ToString(), r.EntityId, r.Reason,
                r.ReporterId.Value, reporter?.Username ?? "—", r.CreatedAt));
        }

        return result;
    }
}
