using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.CreateScene;

internal sealed class CreateSceneCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<CreateSceneCommand, Result<CreateSceneResponse>>
{
    public async Task<Result<CreateSceneResponse>> Handle(CreateSceneCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Name) || command.Name.Length > 200)
            return Error.Validation("Scene.InvalidName", "Название сцены некорректно.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));
        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        var existing = await sceneRepository.GetBySessionAsync(session.Id, ct);
        var scene = Scene.Create(session.Id, command.Name.Trim(), existing.Count);
        await sceneRepository.AddAsync(scene, ct);

        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyActiveSceneChangedAsync(command.SessionId, ct);

        return new CreateSceneResponse(scene.Id, scene.Name);
    }
}
