using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities.Events;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Events.Commands.CreateEvent;

internal sealed class CreateEventCommandHandler(
    IGameEventRepository repo, ICurrentUser currentUser, IUnitOfWork uow)
    : IRequestHandler<CreateEventCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<EventFormat>(request.Format, out var format))
            return Error.Validation("Неверный формат. Допустимо: Online, Offline, Hybrid.");

        var ev = GameEvent.Create(
            currentUser.Id, request.Title, request.Description, request.System,
            format, request.Location, request.OnlineLink,
            request.StartsAt, request.MaxParticipants);

        await repo.AddAsync(ev, ct);
        await uow.SaveChangesAsync(ct);
        return Result<Guid>.Success(ev.Id.Value);
    }
}
