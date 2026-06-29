using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Events;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Events.Commands.RegisterForEvent;

internal sealed class RegisterForEventCommandHandler(
    IGameEventRepository repo, ICurrentUser currentUser, IUnitOfWork uow)
    : IRequestHandler<RegisterForEventCommand, Result>
{
    public async Task<Result> Handle(RegisterForEventCommand request, CancellationToken ct)
    {
        var ev = await repo.GetByIdWithParticipantsAsync(GameEventId.From(request.EventId), ct);
        if (ev is null) return Error.NotFound(nameof(GameEvent));

        if (ev.IsCancelled)
            return Error.Validation("Событие отменено.");

        if (ev.StartsAt < DateTime.UtcNow)
            return Error.Validation("Событие уже прошло.");

        if (ev.IsParticipant(currentUser.Id))
            return Error.Conflict("Participation");

        if (!ev.HasSlot)
            return Error.Validation("Все места заняты.");

        ev.AddParticipant(currentUser.Id);
        await uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
