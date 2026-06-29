using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Events;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Events.Commands.UnregisterFromEvent;

internal sealed class UnregisterFromEventCommandHandler(
    IGameEventRepository repo, ICurrentUser currentUser, IUnitOfWork uow)
    : IRequestHandler<UnregisterFromEventCommand, Result>
{
    public async Task<Result> Handle(UnregisterFromEventCommand request, CancellationToken ct)
    {
        var ev = await repo.GetByIdWithParticipantsAsync(GameEventId.From(request.EventId), ct);
        if (ev is null) return Error.NotFound(nameof(GameEvent));

        if (!ev.IsParticipant(currentUser.Id))
            return Error.NotFound("Participation");

        ev.RemoveParticipant(currentUser.Id);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
