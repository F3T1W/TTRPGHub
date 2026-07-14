using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Initiative.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Commands.UpdateEntry;

internal sealed class UpdateEntryCommandHandler(
    IInitiativeTrackerRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITrackerNotifier notifier,
    InitiativeTrackerSync sync
) : IRequestHandler<UpdateEntryCommand, Result>
{
    public async Task<Result> Handle(UpdateEntryCommand command, CancellationToken ct)
    {
        var tracker = await repository.GetByIdAsync(new InitiativeTrackerId(command.TrackerId), ct);
        if (tracker is null) return Error.NotFound(nameof(InitiativeTracker));
        if (tracker.OwnerId != currentUser.Id) return Error.Unauthorized();

        tracker.UpdateEntry(command.EntryId, command.CurrentHp, command.Status, command.Notes);
        repository.Update(tracker);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyTrackerUpdatedAsync(tracker.Id.Value, TrackerMapper.ToDto(tracker), ct);

        // R.1 — обратный синк: если трекер привязан к сессии стола (LinkedSessionId, см. Q.5) и
        // эта запись привязана к конкретному токену, правка HP/статуса уходит и на карту.
        var entry = tracker.Entries.FirstOrDefault(e => e.Id == command.EntryId);
        if (tracker.LinkedSessionId is not null && entry is not null)
            await sync.PushEntryToTokenAsync(entry, ct);

        return Result.Success();
    }
}
