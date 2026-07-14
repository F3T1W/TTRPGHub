using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;
using TTRPGHub.Features.Initiative.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Initiative.Commands.SyncTrackerFromTable;

public sealed record SyncTrackerFromTableCommand(Guid TrackerId, Guid SessionId) : IRequest<Result<TrackerDetailDto>>;

internal sealed class SyncTrackerFromTableCommandHandler(
    IInitiativeTrackerRepository repository,
    ICurrentUser currentUser,
    InitiativeTrackerSync sync
) : IRequestHandler<SyncTrackerFromTableCommand, Result<TrackerDetailDto>>
{
    public async Task<Result<TrackerDetailDto>> Handle(SyncTrackerFromTableCommand command, CancellationToken ct)
    {
        var tracker = await repository.GetByIdAsync(new InitiativeTrackerId(command.TrackerId), ct);
        if (tracker is null) return Error.NotFound(nameof(InitiativeTracker));
        if (tracker.OwnerId != currentUser.Id) return Error.Unauthorized();

        return await sync.SyncFromSessionAsync(tracker, command.SessionId, ct);
    }
}
