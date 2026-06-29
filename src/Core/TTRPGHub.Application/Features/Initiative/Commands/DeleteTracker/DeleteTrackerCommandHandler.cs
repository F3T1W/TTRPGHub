using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Commands.DeleteTracker;

internal sealed class DeleteTrackerCommandHandler(
    IInitiativeTrackerRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<DeleteTrackerCommand, Result>
{
    public async Task<Result> Handle(DeleteTrackerCommand command, CancellationToken ct)
    {
        var tracker = await repository.GetByIdAsync(new InitiativeTrackerId(command.TrackerId), ct);
        if (tracker is null) return Error.NotFound(nameof(InitiativeTracker));
        if (tracker.OwnerId != currentUser.Id) return Error.Unauthorized();

        repository.Delete(tracker);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
