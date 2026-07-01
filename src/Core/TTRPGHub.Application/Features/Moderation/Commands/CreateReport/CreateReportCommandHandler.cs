using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Moderation.Commands.CreateReport;

internal sealed class CreateReportCommandHandler(
    IContentReportRepository reports,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateReportCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateReportCommand request, CancellationToken ct)
    {
        var report = ContentReport.Create(currentUser.Id, request.EntityType, request.EntityId, request.Reason);
        await reports.AddAsync(report, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return report.Id.Value;
    }
}
