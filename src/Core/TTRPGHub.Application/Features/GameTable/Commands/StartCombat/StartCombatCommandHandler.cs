using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.StartCombat;

internal sealed class StartCombatCommandHandler(
    IGameSessionRepository sessionRepository,
    ISceneRepository sceneRepository,
    ITableTokenRepository tokenRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<StartCombatCommand, Result>
{
    public async Task<Result> Handle(StartCombatCommand command, CancellationToken ct)
    {
        var resolved = await ActiveSceneResolver.ResolveForGmAsync(
            sessionRepository, sceneRepository, new GameSessionId(command.SessionId), currentUser.Id, ct);
        if (resolved.IsFailure)
            return resolved.Error!;

        var scene = resolved.Value!.Scene;
        scene.StartCombat();

        // Сразу ставим ход на первого по инициативе, если у кого-то из токенов она уже задана —
        // иначе GM пришлось бы жать "следующий ход" вхолостую сразу после старта боя.
        var tokens = await tokenRepository.GetBySceneAsync(scene.Id, ct);
        var firstTokenId = InitiativeOrder.Sorted(tokens).FirstOrDefault()?.Id;
        if (firstTokenId is not null)
            scene.SetCombatTurn(firstTokenId, 1);

        sceneRepository.Update(scene);
        await unitOfWork.SaveChangesAsync(ct);
        await notifier.NotifyCombatStateChangedAsync(command.SessionId, true, scene.CombatRound, scene.CombatTurnTokenId, ct);

        return Result.Success();
    }
}
