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
}
