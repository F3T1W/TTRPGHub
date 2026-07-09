using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;


namespace TTRPGHub.Features.Moderation.Commands.ResolveReport;

internal sealed class ResolveReportCommandHandler(
    IContentReportRepository reports,
    IModerationLogRepository moderationLog,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<ResolveReportCommand, Result>
{
    public async Task<Result> Handle(ResolveReportCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<ReportStatus>(request.Status, ignoreCase: true, out var status) || status == ReportStatus.Open)
            return Error.Validation("Status", "Допустимые статусы: Resolved, Dismissed.");

        var report = await reports.GetByIdAsync(ContentReportId.From(request.ReportId), ct);
        if (report is null)
            return Error.NotFound(nameof(ContentReport));

        report.Resolve(currentUser.Id, status);
        reports.Update(report);

        await moderationLog.AddAsync(ModerationLogEntry.Create(
            currentUser.Id, $"ResolveReport:{status}", report.EntityType.ToString(), report.EntityId), ct);

        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
