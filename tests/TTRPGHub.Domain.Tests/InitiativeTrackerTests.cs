using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class InitiativeTrackerTests
{
    private static InitiativeTracker CreateValid() =>
        InitiativeTracker.Create(CampaignId.New(), UserId.New(), "Boss fight");

    [Fact]
    public void SyncFromToken_NewToken_AddsEntry()
    {
        var tracker = CreateValid();
        var tokenId = Guid.NewGuid();

        tracker.SyncFromToken(tokenId, "Goblin", initiative: 15, currentHp: 8, maxHp: 8, armorClass: 16,
            conditionsJson: null, isPlayerCharacter: false);

        var entry = Assert.Single(tracker.Entries);
        Assert.Equal("Goblin", entry.Name);
        Assert.Equal(15, entry.Initiative);
        Assert.Equal(tokenId, entry.LinkedTokenId);
    }

    [Fact]
    public void SyncFromToken_ExistingLinkedToken_UpdatesInPlace()
    {
        var tracker = CreateValid();
        var tokenId = Guid.NewGuid();
        tracker.SyncFromToken(tokenId, "Goblin", 15, 8, 8, 16, null, false);

        tracker.SyncFromToken(tokenId, "Goblin", 15, 3, 8, 16, null, false);

        var entry = Assert.Single(tracker.Entries);
        Assert.Equal(3, entry.CurrentHp);
    }

    [Fact]
    public void SyncFromToken_ZeroHp_DerivesUnconsciousStatus()
    {
        var tracker = CreateValid();
        var tokenId = Guid.NewGuid();

        tracker.SyncFromToken(tokenId, "Goblin", 15, currentHp: 0, maxHp: 8, armorClass: 16,
            conditionsJson: null, isPlayerCharacter: false);

        Assert.Equal(EntryStatus.Unconscious, tracker.Entries.Single().Status);
    }

    [Fact]
    public void SyncFromToken_NullMaxHp_KeepsPreviousMax()
    {
        var tracker = CreateValid();
        var tokenId = Guid.NewGuid();
        tracker.SyncFromToken(tokenId, "Goblin", 15, 8, maxHp: 8, armorClass: 16, conditionsJson: null, isPlayerCharacter: false);

        tracker.SyncFromToken(tokenId, "Goblin", 15, currentHp: 5, maxHp: null, armorClass: 16, conditionsJson: null, isPlayerCharacter: false);

        Assert.Equal(8, tracker.Entries.Single().MaxHp);
    }

    [Fact]
    public void ReorderEntries_SortsBySortOrderDescendingInitiative()
    {
        var tracker = CreateValid();
        tracker.SyncFromToken(Guid.NewGuid(), "Slow", 5, 8, 8, 16, null, false);
        tracker.SyncFromToken(Guid.NewGuid(), "Fast", 20, 8, 8, 16, null, false);

        tracker.ReorderEntries();

        var ordered = tracker.Entries.OrderBy(e => e.SortOrder).ToList();
        Assert.Equal("Fast", ordered[0].Name);
        Assert.Equal("Slow", ordered[1].Name);
    }

    [Fact]
    public void LinkSession_SetsLinkedSessionId()
    {
        var tracker = CreateValid();
        var sessionId = Guid.NewGuid();

        tracker.LinkSession(sessionId);

        Assert.Equal(sessionId, tracker.LinkedSessionId);
    }
}
