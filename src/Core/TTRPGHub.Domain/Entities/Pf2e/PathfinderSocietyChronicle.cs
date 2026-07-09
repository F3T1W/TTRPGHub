using TTRPGHub.Common;

namespace TTRPGHub.Entities.Pf2e;

// N.3 — chronicle sheet организованной игры Pathfinder Society: запись за один сыгранный
// сценарий (золото/AP/фракция/использованные буны). В отличие от Pf2eStatsJson (единый
// jsonb-блоб "текущего состояния" листа) это лог с собственной идентичностью — нужно листать
// историю по сценариям и генерировать PDF на каждую запись отдельно, поэтому отдельная таблица,
// а не поле на Character.
public sealed class PathfinderSocietyChronicle : Entity<PathfinderSocietyChronicleId>
{
    public new PathfinderSocietyChronicleId Id { get; private set; }
    public CharacterId CharacterId { get; private set; }
    public string ScenarioName { get; private set; } = "";
    public DateOnly SessionDate { get; private set; }
    public string? GmName { get; private set; }
    public string? Faction { get; private set; }
    public int GoldEarned { get; private set; }
    public int AchievementPoints { get; private set; }
    public string? BoonsUsed { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private init; }

    private PathfinderSocietyChronicle() { }

    public static Result<PathfinderSocietyChronicle> Create(
        CharacterId characterId, string scenarioName, DateOnly sessionDate, string? gmName,
        string? faction, int goldEarned, int achievementPoints, string? boonsUsed, string? notes)
    {
        if (string.IsNullOrWhiteSpace(scenarioName))
            return Error.Validation(nameof(ScenarioName), "Название сценария не может быть пустым.");

        return new PathfinderSocietyChronicle
        {
            Id = PathfinderSocietyChronicleId.New(),
            CharacterId = characterId,
            ScenarioName = scenarioName.Trim(),
            SessionDate = sessionDate,
            GmName = string.IsNullOrWhiteSpace(gmName) ? null : gmName.Trim(),
            Faction = string.IsNullOrWhiteSpace(faction) ? null : faction.Trim(),
            GoldEarned = Math.Max(0, goldEarned),
            AchievementPoints = Math.Max(0, achievementPoints),
            BoonsUsed = string.IsNullOrWhiteSpace(boonsUsed) ? null : boonsUsed.Trim(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = DateTime.UtcNow,
        };
    }
}
