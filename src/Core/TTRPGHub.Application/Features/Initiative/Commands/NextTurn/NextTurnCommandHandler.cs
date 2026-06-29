using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Commands.NextTurn;

internal sealed class NextTurnCommandHandler(
    IInitiativeTrackerRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITrackerNotifier notifier
) : IRequestHandler<NextTurnCommand, Result>
{
    public async Task<Result> Handle(NextTurnCommand command, CancellationToken ct)
    {
        var tracker = await repository.GetByIdAsync(new InitiativeTrackerId(command.TrackerId), ct);
        if (tracker is null) return Error.NotFound(nameof(InitiativeTracker));
        if (tracker.OwnerId != currentUser.Id) return Error.Unauthorized();

        tracker.NextTurn();
        repository.Update(tracker);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyTrackerUpdatedAsync(tracker.Id.Value, TrackerMapper.ToDto(tracker), ct);
        return Result.Success();
    }
}
