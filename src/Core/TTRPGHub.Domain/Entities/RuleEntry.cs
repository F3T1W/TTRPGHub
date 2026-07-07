using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class RuleEntry : Entity<RuleEntryId>
{
    public GameSystemId SystemId { get; private set; }
    public RuleCategory Category { get; private set; }
    public string Slug { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Summary { get; private set; }
    public string? ContentMarkdown { get; private set; }

    // Гибкая схема под конкретную категорию/систему (JSONB): для Spell — уровень/школа/компоненты,
    // для Monster — характеристики/действия, для Class — таблица прогрессии и т.д.
    // Строгая реляционная схема под каждую комбинацию категория×система не масштабируется
    // на неограниченное число кастомных систем — см. ROADMAP.md.
    public string StatsJson { get; private set; } = "{}";

    public string[] Tags { get; private set; } = [];
    public bool IsHomebrew { get; private set; }
    public string Source { get; private set; } = null!;
    public DateTime CreatedAt { get; private init; }
    public DateTime UpdatedAt { get; private set; }

    private RuleEntry() { }

    public static RuleEntry Create(
        GameSystemId systemId, RuleCategory category, string slug, string title,
        string? summary, string? contentMarkdown, string statsJson,
        string[] tags, bool isHomebrew, string source)
    {
        var now = DateTime.UtcNow;
        return new RuleEntry
        {
            Id = RuleEntryId.New(),
            SystemId = systemId,
            Category = category,
            Slug = slug,
            Title = title,
            Summary = summary,
            ContentMarkdown = contentMarkdown,
            StatsJson = statsJson,
            Tags = tags,
            IsHomebrew = isHomebrew,
            Source = source,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(string title, string? summary, string? contentMarkdown, string statsJson, string[] tags)
    {
        Title = title;
        Summary = summary;
        ContentMarkdown = contentMarkdown;
        StatsJson = statsJson;
        Tags = tags;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum RuleCategory { Spell, Monster, Class, Race, Feat, Condition, Equipment, Background, Rule, Action }
