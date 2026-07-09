using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.SetVariantRules;

internal sealed class SetVariantRulesCommandHandler(
    IGameSessionRepository sessionRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<SetVariantRulesCommand, Result>
{
    public async Task<Result> Handle(SetVariantRulesCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var error = session.SetVariantRules(
            currentUser.Id, command.ProficiencyWithoutLevel, command.AutomaticBonusProgression,
            command.FreeArchetype, command.GradualAbilityBoosts, command.StaminaVariant);
        if (error is not null)
            return error;

        sessionRepository.Update(session);
        await unitOfWork.SaveChangesAsync(ct);

        await notifier.NotifyVariantRulesChangedAsync(
            command.SessionId, session.ProficiencyWithoutLevel, session.AutomaticBonusProgression,
            session.FreeArchetype, session.GradualAbilityBoosts, session.StaminaVariant, ct);
        return Result.Success();
    }
}
