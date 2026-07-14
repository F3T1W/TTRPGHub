using System.Text.Json;

namespace TTRPGHub.Entities;

public enum EntryStatus { Active, Unconscious, Dead }

public sealed class InitiativeEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public int Initiative { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int ArmorClass { get; set; }
    public EntryStatus Status { get; set; } = EntryStatus.Active;
    public bool IsPlayerCharacter { get; set; }
    public string? Notes { get; set; }
    public int SortOrder { get; set; }
    public Guid? LinkedTokenId { get; set; }
    public string? ConditionsJson { get; set; }

    public static EntryStatus DeriveStatus(int currentHp, string? conditionsJson)
    {
        if (HasConditionSlug(conditionsJson, "dead"))
            return EntryStatus.Dead;

        if (currentHp <= 0 || HasConditionSlug(conditionsJson, "unconscious") || HasConditionSlug(conditionsJson, "dying"))
            return EntryStatus.Unconscious;

        return EntryStatus.Active;
    }

    private static bool HasConditionSlug(string? conditionsJson, string slug)
    {
        if (string.IsNullOrWhiteSpace(conditionsJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(conditionsJson);
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (item.TryGetProperty("slug", out var s)
                    && string.Equals(s.GetString(), slug, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }
        catch (JsonException) { /* ignore malformed snapshot */ }

        return false;
    }
}
