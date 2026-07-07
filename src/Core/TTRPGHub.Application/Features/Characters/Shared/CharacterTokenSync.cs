using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Shared;

// Токен персонажа на столе — "привязанный" (linked) actor, как в Foundry: правки на листе
// персонажа (лечение вне боя, level-up) должны сразу отражаться на его токене на любой карте,
// где он сейчас стоит. Токены монстров этим не затрагиваются — они намеренно независимы (H.4).
internal static class CharacterTokenSync
{
    internal static async Task<IReadOnlyList<(Guid SessionId, TableTokenDto Dto)>> SyncTokensAsync(
        Character character, ITableTokenRepository tokenRepository, CancellationToken ct)
    {
        var tokens = await tokenRepository.GetByCombatantAsync(TokenCombatantType.Character, character.Id.Value, ct);
        if (tokens.Count == 0)
            return [];

        var results = new List<(Guid, TableTokenDto)>(tokens.Count);
        foreach (var token in tokens)
        {
            token.SyncFromCharacter(character.CurrentHitPoints, character.MaxHitPoints, character.ArmorClass);
            tokenRepository.Update(token);
            results.Add((token.SessionId.Value, TableTokenMapper.ToDto(token, canMove: true)));
        }

        return results;
    }
}
