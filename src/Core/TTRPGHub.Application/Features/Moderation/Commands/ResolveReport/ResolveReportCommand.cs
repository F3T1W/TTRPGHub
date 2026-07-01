using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Moderation.Commands.ResolveReport;

public sealed record ResolveReportCommand(Guid ReportId, string Status) : IRequest<Result>;
