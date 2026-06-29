using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Commands.ChangeSessionStatus;

internal sealed class ChangeSessionStatusCommandHandler(
    IGameSessionRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<ChangeSessionStatusCommand, Result>
{
    public async Task<Result> Handle(ChangeSessionStatusCommand command, CancellationToken ct)
    {
        var session = await repository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null) return Error.NotFound(nameof(GameSession));

        Error? error = command.NewStatus switch
        {
            SessionStatus.InProgress => session.Start(currentUser.Id),
            SessionStatus.Completed  => session.Complete(currentUser.Id),
            SessionStatus.Cancelled  => session.Cancel(currentUser.Id),
            _ => Error.Validation("Session.Status", "Недопустимый статус.")
        };
        if (error is not null) return error;

        repository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
