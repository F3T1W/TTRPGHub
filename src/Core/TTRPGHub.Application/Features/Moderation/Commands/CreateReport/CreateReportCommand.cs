using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Moderation;

namespace TTRPGHub.Features.Moderation.Commands.CreateReport;

public sealed record CreateReportCommand(ReportedEntityType EntityType, Guid EntityId, string Reason)
    : IRequest<Result<Guid>>;
