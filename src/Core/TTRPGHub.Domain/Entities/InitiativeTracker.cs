using TTRPGHub.Common;

namespace TTRPGHub.Entities;

public sealed class InitiativeTracker : Entity<InitiativeTrackerId>
{
    private readonly List<InitiativeEntry> _entries = [];

    public new InitiativeTrackerId Id { get; private set; }
    public CampaignId CampaignId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Name { get; private set; } = "";
    public int Round { get; private set; } = 1;
    public int ActiveEntryIndex { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IReadOnlyList<InitiativeEntry> Entries => _entries.AsReadOnly();

    private InitiativeTracker() { }

    public static InitiativeTracker Create(CampaignId campaignId, UserId ownerId, string name) =>
        new()
        {
            Id                = InitiativeTrackerId.New(),
            CampaignId        = campaignId,
            OwnerId           = ownerId,
            Name              = name,
            Round             = 1,
            ActiveEntryIndex  = 0,
            IsActive          = false,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow,
        };

    public void SetEntries(List<InitiativeEntry> entries)
    {
        _entries.Clear();
        var sorted = entries.OrderByDescending(e => e.Initiative).ToList();
        for (var i = 0; i < sorted.Count; i++) sorted[i].SortOrder = i;
        _entries.AddRange(sorted);
        ActiveEntryIndex = 0;
        Round     = 1;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateEntry(Guid entryId, int currentHp, EntryStatus status, string? notes)
    {
        var entry = _entries.FirstOrDefault(e => e.Id == entryId);
        if (entry is null) return;
        entry.CurrentHp = currentHp;
        entry.Status    = status;
        entry.Notes     = notes;
        UpdatedAt       = DateTime.UtcNow;
    }

    public void Start()
    {
        IsActive         = true;
        ActiveEntryIndex = 0;
        Round            = 1;
        UpdatedAt        = DateTime.UtcNow;
    }

    public void NextTurn()
    {
        if (_entries.Count == 0) return;
        ActiveEntryIndex++;
        if (ActiveEntryIndex >= _entries.Count)
        {
            ActiveEntryIndex = 0;
            Round++;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void PreviousTurn()
    {
        if (_entries.Count == 0) return;
        ActiveEntryIndex--;
        if (ActiveEntryIndex < 0)
        {
            ActiveEntryIndex = _entries.Count - 1;
            if (Round > 1) Round--;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        Name      = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
