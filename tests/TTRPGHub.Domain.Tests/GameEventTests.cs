using TTRPGHub.Entities;
using TTRPGHub.Entities.Events;

namespace TTRPGHub.Domain.Tests;

public class GameEventTests
{
    private static GameEvent CreateEvent(int maxParticipants = 4, DateTime? startsAt = null) => GameEvent.Create(
        UserId.New(), "Open table", "Bring a level 1 character", "pf2e",
        EventFormat.Online, null, "https://discord.gg/test",
        startsAt ?? DateTime.UtcNow.AddDays(3), maxParticipants);

    [Fact]
    public void Create_StartsNotCancelledWithNoParticipants()
    {
        var ev = CreateEvent();

        Assert.False(ev.IsCancelled);
        Assert.Empty(ev.Participants);
    }

    [Fact]
    public void HasSlot_BelowCapacity_IsTrue()
    {
        var ev = CreateEvent(maxParticipants: 2);
        ev.AddParticipant(UserId.New());

        Assert.True(ev.HasSlot);
    }

    [Fact]
    public void HasSlot_AtCapacity_IsFalse()
    {
        var ev = CreateEvent(maxParticipants: 1);
        ev.AddParticipant(UserId.New());

        Assert.False(ev.HasSlot);
    }

    [Fact]
    public void IsParticipant_UnknownUser_IsFalse()
    {
        var ev = CreateEvent();

        Assert.False(ev.IsParticipant(UserId.New()));
    }

    [Fact]
    public void RemoveParticipant_UnknownUser_IsNoOp()
    {
        var ev = CreateEvent();

        ev.RemoveParticipant(UserId.New());

        Assert.Empty(ev.Participants);
    }

    [Fact]
    public void AddThenRemoveParticipant_LeavesNoTrace()
    {
        var ev = CreateEvent();
        var userId = UserId.New();
        ev.AddParticipant(userId);

        ev.RemoveParticipant(userId);

        Assert.False(ev.IsParticipant(userId));
        Assert.True(ev.HasSlot);
    }

    [Fact]
    public void Cancel_SetsIsCancelled()
    {
        var ev = CreateEvent();

        ev.Cancel();

        Assert.True(ev.IsCancelled);
    }
}
