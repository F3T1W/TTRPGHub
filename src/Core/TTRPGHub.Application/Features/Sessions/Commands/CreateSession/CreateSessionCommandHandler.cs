using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Commands.CreateSession;

internal sealed class CreateSessionCommandHandler(
    IGameSessionRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<CreateSessionCommand, Result<CreateSessionResponse>>
{
    public async Task<Result<CreateSessionResponse>> Handle(CreateSessionCommand command, CancellationToken ct)
    {
        var session = GameSession.Create(
            currentUser.Id,
            command.Title,
            command.Description,
            command.System,
            command.MaxPlayers,
            command.ScheduledAt.ToUniversalTime());

        await repository.AddAsync(session, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new CreateSessionResponse(session.Id.Value, session.Title);
    }
}
