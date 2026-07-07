using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.GameTable.Commands.UpdateTokenStats;

internal sealed class UpdateTokenStatsCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableTokenRepository tokenRepository,
    ICharacterRepository characterRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<UpdateTokenStatsCommand, Result>
{
    public async Task<Result> Handle(UpdateTokenStatsCommand command, CancellationToken ct)
    {
        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        var token = await tokenRepository.GetByIdAsync(command.TokenId, ct);
        if (token is null || token.SessionId != session.Id)
            return Error.NotFound(nameof(TableToken));

        var isOrganizer = session.OrganizerId == currentUser.Id;
        if (!token.CanBeMovedBy(currentUser.Id, isOrganizer))
            return Error.Unauthorized();

        if (command.CurrentHp is { } hp)
        {
            token.UpdateHp(hp);

            // Токен персонажа игрока — "привязанный" (linked) actor как в Foundry: урон/лечение
            // в бою пишутся сразу и на лист персонажа, а не остаются только на копии токена.
            // Токены монстров намеренно независимы (unlinked) — см. ROADMAP H.4.
            if (token.CombatantType == TokenCombatantType.Character && token.CombatantId is { } charId)
            {
                var character = await characterRepository.GetByIdAsync(new CharacterId(charId), ct);
                if (character is not null)
                {
                    character.SetCurrentHitPoints(token.CurrentHp ?? hp);
                    characterRepository.Update(character);
                }
            }
        }

        // Менять размер жетона может только GM — игрок управляет только HP своего токена.
        if ((command.Width is not null || command.Height is not null) && !isOrganizer)
            return Error.Unauthorized();
        if (command.Width is { } w || command.Height is { } h)
            token.Resize(command.Width ?? token.Width, command.Height ?? token.Height);

        // Поворот — косметика, разрешена и владельцу токена, не только GM (как HP).
        if (command.Rotation is { } rotation)
            token.Rotate(rotation);

        // Инициатива — как HP, задаёт и владелец токена (свой бросок), и GM (для NPC/монстров).
        if (command.SetInitiative)
            token.SetInitiative(command.Initiative);

        // Тёмное зрение — как инициатива, доступно и владельцу токена, и GM (J.3).
        if (command.HasDarkvision is { } darkvision)
            token.SetDarkvision(darkvision);

        if (command.HasLowLightVision is { } lowLight)
            token.SetLowLightVision(lowLight);

        tokenRepository.Update(token);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = TableTokenMapper.ToDto(token, canMove: true);
        await notifier.NotifyTokenUpdatedAsync(command.SessionId, dto, ct);

        return Result.Success();
    }
}
