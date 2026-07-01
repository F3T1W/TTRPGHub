using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Moderation.Queries.GetOpenReports;

public sealed record GetOpenReportsQuery : IRequest<Result<List<ContentReportDto>>>;

public sealed record ContentReportDto(
    Guid Id, string EntityType, Guid EntityId, string Reason,
    Guid ReporterId, string ReporterUsername, DateTime CreatedAt);
