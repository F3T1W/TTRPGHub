using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Commands.ImportSession;

internal sealed class ImportSessionCommandHandler(
    IGameSessionRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<ImportSessionCommand, Result<ImportSessionResponse>>
{
    public async Task<Result<ImportSessionResponse>> Handle(ImportSessionCommand cmd, CancellationToken ct)
    {
        var session = GameSession.Create(
            currentUser.Id,
            cmd.Title,
            cmd.Description,
            cmd.System,
            cmd.MaxPlayers,
            cmd.ScheduledAt.ToUniversalTime(),
            SessionFormat.Online,
            location: null);

        await repository.AddAsync(session, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return new ImportSessionResponse(session.Id.Value, session.Title);
    }
}
