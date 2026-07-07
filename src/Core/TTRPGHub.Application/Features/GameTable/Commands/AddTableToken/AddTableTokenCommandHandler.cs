using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;
using TTRPGHub.Repositories.Dnd5e;

namespace TTRPGHub.Features.GameTable.Commands.AddTableToken;

internal sealed class AddTableTokenCommandHandler(
    IGameSessionRepository sessionRepository,
    ITableTokenRepository tokenRepository,
    ICharacterRepository characterRepository,
    IPf2eMonsterRepository pf2eMonsterRepository,
    IDnd5eMonsterRepository dnd5eMonsterRepository,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICurrentUser currentUser
) : IRequestHandler<AddTableTokenCommand, Result<TableTokenDto>>
{
    public async Task<Result<TableTokenDto>> Handle(AddTableTokenCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Label) || command.Label.Length > 100)
            return Error.Validation("TableToken.Invalid", "Название жетона некорректно.");

        if (!Enum.TryParse<TokenCombatantType>(command.CombatantType, ignoreCase: true, out var combatantType))
            return Error.Validation("TableToken.InvalidCombatantType", "Неизвестный тип источника статов.");

        var session = await sessionRepository.GetByIdAsync(new GameSessionId(command.SessionId), ct);
        if (session is null)
            return Error.NotFound(nameof(GameSession));

        if (session.OrganizerId != currentUser.Id)
            return Error.Unauthorized();

        if (session.ActiveSceneId is not { } activeSceneId)
            return Error.Validation("Scene.NoActiveScene", "У сессии нет активной сцены.");

        UserId? ownerId = command.OwnerUserId is { } id ? new UserId(id) : null;
        if (ownerId is not null && !session.IsParticipant(ownerId.Value))
            return Error.Validation("TableToken.InvalidOwner", "Владелец жетона должен быть участником сессии.");

        var label = command.Label.Trim();
        var imageUrl = command.ImageUrl;
        int? currentHp = null, maxHp = null, armorClass = null;

        if (combatantType != TokenCombatantType.None && command.CombatantId is not { } combatantId)
            return Error.Validation("TableToken.MissingCombatantId", "Не указан источник статов.");

        switch (combatantType)
        {
            case TokenCombatantType.Character:
                var character = await characterRepository.GetByIdAsync(new CharacterId(command.CombatantId!.Value), ct);
                if (character is null) return Error.NotFound(nameof(Character));
                label = character.Name;
                imageUrl ??= character.AvatarUrl;
                currentHp = character.CurrentHitPoints;
                maxHp = character.MaxHitPoints;
                armorClass = character.ArmorClass;
                break;

            case TokenCombatantType.Pf2eMonster:
                var pf2eMonster = await pf2eMonsterRepository.GetByIdAsync(new Pf2eMonsterId(command.CombatantId!.Value), ct);
                if (pf2eMonster is null) return Error.NotFound(nameof(Pf2eMonster));
                label = pf2eMonster.Name;
                maxHp = currentHp = pf2eMonster.HitPoints;
                armorClass = pf2eMonster.ArmorClass;
                break;

            case TokenCombatantType.Dnd5eMonster:
                var dnd5eMonster = await dnd5eMonsterRepository.GetByIdAsync(new Dnd5eMonsterId(command.CombatantId!.Value), ct);
                if (dnd5eMonster is null) return Error.NotFound(nameof(Dnd5eMonster));
                label = dnd5eMonster.Name;
                maxHp = currentHp = dnd5eMonster.HitPoints;
                armorClass = dnd5eMonster.ArmorClass;
                break;
        }

        var token = TableToken.Create(
            session.Id, activeSceneId, label, imageUrl, command.Color,
            command.X, command.Y, ownerId,
            command.Width, command.Height,
            combatantType, command.CombatantId,
            currentHp, maxHp, armorClass);

        await tokenRepository.AddAsync(token, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var dto = TableTokenMapper.ToDto(token, canMove: true);
        await notifier.NotifyTokenAddedAsync(command.SessionId, dto, ct);

        return dto;
    }
}
