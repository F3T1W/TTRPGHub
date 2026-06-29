using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Commands.StartTracker;

internal sealed class StartTrackerCommandHandler(
    IInitiativeTrackerRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITrackerNotifier notifier
) : IRequestHandler<StartTrackerCommand, Result>
{
    public async Task<Result> Handle(StartTrackerCommand command, CancellationToken ct)
    {
        var tracker = await repository.GetByIdAsync(new InitiativeTrackerId(command.TrackerId), ct);
        if (tracker is null) return Error.NotFound(nameof(InitiativeTracker));
        if (tracker.OwnerId != currentUser.Id) return Error.Unauthorized();

        tracker.Start();
        repository.Update(tracker);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyTrackerUpdatedAsync(tracker.Id.Value, TrackerMapper.ToDto(tracker), ct);
        return Result.Success();
    }
}
