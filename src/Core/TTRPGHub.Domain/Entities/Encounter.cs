using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public enum EncounterDifficulty { Trivial, Easy, Medium, Hard, Deadly }

public sealed class Encounter : Entity<EncounterId>
{
    private readonly List<EncounterEntry> _entries = [];

    public new EncounterId Id { get; private set; }
    public CampaignId CampaignId { get; private set; }
    public UserId CreatedById { get; private set; }
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public EncounterDifficulty Difficulty { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<EncounterEntry> Entries => _entries.AsReadOnly();

    private Encounter() { }

    public static Encounter Create(CampaignId campaignId, UserId createdById,
        string title, string? description, EncounterDifficulty difficulty, string? notes)
    {
        return new Encounter
        {
            Id          = EncounterId.New(),
            CampaignId  = campaignId,
            CreatedById = createdById,
            Title       = title,
            Description = description,
            Difficulty  = difficulty,
            Notes       = notes,
            CreatedAt   = DateTime.UtcNow,
            UpdatedAt   = DateTime.UtcNow,
        };
    }

    public Result Update(string title, string? description, EncounterDifficulty difficulty, string? notes)
    {
        Title       = title;
        Description = description;
        Difficulty  = difficulty;
        Notes       = notes;
        UpdatedAt   = DateTime.UtcNow;
        return Result.Success();
    }

    public void SetEntries(IEnumerable<EncounterEntry> entries)
    {
        _entries.Clear();
        _entries.AddRange(entries);
        UpdatedAt = DateTime.UtcNow;
    }
}
