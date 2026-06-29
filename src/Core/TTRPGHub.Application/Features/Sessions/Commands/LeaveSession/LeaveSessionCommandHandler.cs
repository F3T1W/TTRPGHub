using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Commands.LeaveSession;

internal sealed class LeaveSessionCommandHandler(
    IGameSessionRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<LeaveSessionCommand, Result>
{
    public async Task<Result> Handle(LeaveSessionCommand command, CancellationToken ct)
    {
        var session = await repository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null) return Error.NotFound(nameof(GameSession));

        var error = session.Leave(currentUser.Id);
        if (error is not null) return error;

        repository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
