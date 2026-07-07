using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Sessions.Commands.CreateSession;

internal sealed class CreateSessionCommandHandler(
    IGameSessionRepository repository,
    ISceneRepository sceneRepository,
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
            command.ScheduledAt.ToUniversalTime(),
            command.Format,
            command.Location);

        await repository.AddAsync(session, ct);

        // J.4 — каждая сессия стартует с одной сценой по умолчанию, чтобы игровой стол сразу был
        // рабочим (не требует от ГМ обязательного шага "создать сцену" перед первым использованием).
        var scene = Scene.Create(session.Id, "Сцена 1", sortOrder: 0);
        await sceneRepository.AddAsync(scene, ct);
        session.SetActiveScene(currentUser.Id, scene.Id);

        await unitOfWork.SaveChangesAsync(ct);

        return new CreateSessionResponse(session.Id.Value, session.Title);
    }
}
