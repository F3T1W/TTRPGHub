using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Commands.LinkTrackerSession;

public sealed record LinkTrackerSessionCommand(Guid TrackerId, Guid? SessionId) : IRequest<Result<TrackerDetailDto>>;

internal sealed class LinkTrackerSessionCommandHandler(
    IInitiativeTrackerRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITrackerNotifier notifier
) : IRequestHandler<LinkTrackerSessionCommand, Result<TrackerDetailDto>>
{
    public async Task<Result<TrackerDetailDto>> Handle(LinkTrackerSessionCommand command, CancellationToken ct)
    {
        var tracker = await repository.GetByIdAsync(new InitiativeTrackerId(command.TrackerId), ct);
        if (tracker is null) return Error.NotFound(nameof(InitiativeTracker));
        if (tracker.OwnerId != currentUser.Id) return Error.Unauthorized();

        tracker.LinkSession(command.SessionId);
        repository.Update(tracker);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = TrackerMapper.ToDto(tracker);
        await notifier.NotifyTrackerUpdatedAsync(tracker.Id.Value, dto, ct);
        return Result<TrackerDetailDto>.Success(dto);
    }
}
