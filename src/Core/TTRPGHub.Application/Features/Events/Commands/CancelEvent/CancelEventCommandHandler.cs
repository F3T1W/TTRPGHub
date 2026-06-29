using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Events;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Events.Commands.CancelEvent;

internal sealed class CancelEventCommandHandler(
    IGameEventRepository repo, ICurrentUser currentUser, IUnitOfWork uow)
    : IRequestHandler<CancelEventCommand, Result>
{
    public async Task<Result> Handle(CancelEventCommand request, CancellationToken ct)
    {
        var ev = await repo.GetByIdWithParticipantsAsync(GameEventId.From(request.EventId), ct);
        if (ev is null) return Error.NotFound(nameof(GameEvent));

        if (ev.OrganizerId != currentUser.Id)
            return Error.Forbidden();

        ev.Cancel();
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
