using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class JournalEntryTests
{
    private static JournalEntry CreateEntry() =>
        JournalEntry.Create(GameSessionId.New(), UserId.New(), "GM secret", "Body");

    [Fact]
    public void IsVisibleTo_Organizer_AlwaysSeesEntryEvenUnpublished()
    {
        var entry = CreateEntry();

        Assert.True(entry.IsVisibleTo(isOrganizer: true, UserId.New(), visibleToUserIds: null));
    }

    [Fact]
    public void IsVisibleTo_NonOrganizer_UnpublishedEntry_IsHidden()
    {
        var entry = CreateEntry();

        Assert.False(entry.IsVisibleTo(isOrganizer: false, UserId.New(), visibleToUserIds: null));
    }

    [Fact]
    public void IsVisibleTo_NonOrganizer_PublishedWithNoRestriction_IsVisible()
    {
        var entry = CreateEntry();
        entry.SetPublished(true);

        Assert.True(entry.IsVisibleTo(isOrganizer: false, UserId.New(), visibleToUserIds: null));
    }

    [Fact]
    public void IsVisibleTo_NonOrganizer_PublishedAndUserInList_IsVisible()
    {
        var entry = CreateEntry();
        entry.SetPublished(true);
        var userId = UserId.New();

        Assert.True(entry.IsVisibleTo(isOrganizer: false, userId, visibleToUserIds: [userId.Value]));
    }

    [Fact]
    public void IsVisibleTo_NonOrganizer_PublishedButUserNotInList_IsHidden()
    {
        var entry = CreateEntry();
        entry.SetPublished(true);

        Assert.False(entry.IsVisibleTo(isOrganizer: false, UserId.New(), visibleToUserIds: [UserId.New().Value]));
    }

    [Fact]
    public void SetPublished_False_HidesFromNonOrganizerAgain()
    {
        var entry = CreateEntry();
        entry.SetPublished(true);
        entry.SetPublished(false);

        Assert.False(entry.IsVisibleTo(isOrganizer: false, UserId.New(), visibleToUserIds: null));
    }
}
