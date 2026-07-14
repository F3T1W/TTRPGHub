using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class EncounterTests
{
    [Fact]
    public void Create_StartsWithNoEntries()
    {
        var encounter = Encounter.Create(CampaignId.New(), UserId.New(), "Ambush", null, EncounterDifficulty.Medium, null);

        Assert.Empty(encounter.Entries);
    }

    [Fact]
    public void SetEntries_ReplacesExistingListRatherThanMerging()
    {
        var encounter = Encounter.Create(CampaignId.New(), UserId.New(), "Ambush", null, EncounterDifficulty.Medium, null);
        encounter.SetEntries([new EncounterEntry { Name = "Goblin", Count = 4 }]);

        encounter.SetEntries([new EncounterEntry { Name = "Owlbear", Count = 1 }]);

        Assert.Single(encounter.Entries);
        Assert.Equal("Owlbear", encounter.Entries[0].Name);
    }

    [Fact]
    public void SetEntries_EmptyList_ClearsEntries()
    {
        var encounter = Encounter.Create(CampaignId.New(), UserId.New(), "Ambush", null, EncounterDifficulty.Medium, null);
        encounter.SetEntries([new EncounterEntry { Name = "Goblin", Count = 4 }]);

        encounter.SetEntries([]);

        Assert.Empty(encounter.Entries);
    }

    [Fact]
    public void Update_ChangesFieldsAndBumpsUpdatedAt()
    {
        var encounter = Encounter.Create(CampaignId.New(), UserId.New(), "Ambush", "Old description", EncounterDifficulty.Easy, null);
        var before = encounter.UpdatedAt;

        var result = encounter.Update("Ambush 2.0", "New description", EncounterDifficulty.Deadly, "Beware traps");

        Assert.True(result.IsSuccess);
        Assert.Equal("Ambush 2.0", encounter.Title);
        Assert.Equal("New description", encounter.Description);
        Assert.Equal(EncounterDifficulty.Deadly, encounter.Difficulty);
        Assert.Equal("Beware traps", encounter.Notes);
        Assert.True(encounter.UpdatedAt >= before);
    }
}
