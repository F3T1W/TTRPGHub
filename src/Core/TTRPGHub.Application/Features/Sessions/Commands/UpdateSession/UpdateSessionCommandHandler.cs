using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Commands.UpdateSession;

internal sealed class UpdateSessionCommandHandler(
    IGameSessionRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<UpdateSessionCommand, Result>
{
    public async Task<Result> Handle(UpdateSessionCommand command, CancellationToken ct)
    {
        var session = await repository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null) return Error.NotFound(nameof(GameSession));
        if (session.OrganizerId != currentUser.Id) return Error.Unauthorized();

        session.Update(
            command.Title, command.Description, command.System, command.MaxPlayers,
            command.ScheduledAt.ToUniversalTime(), command.Format, command.Location);
        repository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
