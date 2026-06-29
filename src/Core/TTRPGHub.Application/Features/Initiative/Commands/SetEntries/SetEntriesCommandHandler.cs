using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Commands.SetEntries;

internal sealed class SetEntriesCommandHandler(
    IInitiativeTrackerRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    ITrackerNotifier notifier
) : IRequestHandler<SetEntriesCommand, Result>
{
    public async Task<Result> Handle(SetEntriesCommand command, CancellationToken ct)
    {
        var tracker = await repository.GetByIdAsync(new InitiativeTrackerId(command.TrackerId), ct);
        if (tracker is null) return Error.NotFound(nameof(InitiativeTracker));
        if (tracker.OwnerId != currentUser.Id) return Error.Unauthorized();

        tracker.SetEntries(command.Entries.Select(e => new InitiativeEntry
        {
            Name              = e.Name,
            Initiative        = e.Initiative,
            MaxHp             = e.MaxHp,
            CurrentHp         = e.CurrentHp,
            ArmorClass        = e.ArmorClass,
            IsPlayerCharacter = e.IsPlayerCharacter,
            Notes             = e.Notes,
        }).ToList());

        repository.Update(tracker);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyTrackerUpdatedAsync(tracker.Id.Value, TrackerMapper.ToDto(tracker), ct);
        return Result.Success();
    }

}
