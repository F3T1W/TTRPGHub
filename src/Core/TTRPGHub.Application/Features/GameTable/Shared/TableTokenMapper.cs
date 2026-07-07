using System.Text.Json;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.GameTable.Shared;

internal static class TableTokenMapper
{
    internal static TableTokenDto ToDto(TableToken token, bool canMove) => new(
        token.Id, token.Label, token.ImageUrl, token.Color, token.X, token.Y, token.Width, token.Height,
        token.Rotation, token.OwnerId?.Value, canMove, token.CombatantType.ToString(), token.CombatantId,
        token.CurrentHp, token.MaxHp, token.ArmorClass,
        token.Conditions.Select(c => new TokenConditionDto(c.Id, c.Slug, c.Name, c.Value)).ToList(),
        token.Initiative, token.HasDarkvision, token.HasLowLightVision, ParseVisibleTo(token.VisibleToJson));

    internal static List<Guid>? ParseVisibleTo(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<List<Guid>>(json);
}
