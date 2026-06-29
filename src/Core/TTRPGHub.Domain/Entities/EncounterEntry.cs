namespace TTRPGHub.Entities;

public sealed class EncounterEntry
{
    public string Name { get; set; } = "";
    public int Count { get; set; } = 1;
    public string? Notes { get; set; }
}
