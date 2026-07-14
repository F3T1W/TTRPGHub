using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class GameSessionTests
{
    private static GameSession CreateValid(out UserId organizerId, int maxPlayers = 4)
    {
        organizerId = UserId.New();
        return GameSession.Create(
            organizerId, "Curse of the Crimson Throne", null, "pf2e",
            maxPlayers, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
    }

    [Fact]
    public void Create_AddsOrganizerAsParticipant()
    {
        var session = CreateValid(out var organizerId);

        Assert.True(session.IsParticipant(organizerId));
    }

    [Fact]
    public void Join_AddsPlayerAsParticipant()
    {
        var session = CreateValid(out _);
        var playerId = UserId.New();

        var error = session.Join(playerId);

        Assert.Null(error);
        Assert.True(session.IsParticipant(playerId));
    }

    [Fact]
    public void Join_Twice_Fails()
    {
        var session = CreateValid(out _);
        var playerId = UserId.New();
        session.Join(playerId);

        var error = session.Join(playerId);

        Assert.NotNull(error);
    }

    [Fact]
    public void Join_WhenFull_Fails()
    {
        var session = CreateValid(out _, maxPlayers: 1);

        var error = session.Join(UserId.New());

        Assert.NotNull(error);
    }

    [Fact]
    public void ShareMacro_AddsIdOnce()
    {
        var session = CreateValid(out _);
        var macroId = Guid.NewGuid();

        session.ShareMacro(macroId);
        session.ShareMacro(macroId);

        Assert.Single(session.SharedMacroIds);
    }

    [Fact]
    public void UnshareMacro_RemovesId()
    {
        var session = CreateValid(out _);
        var macroId = Guid.NewGuid();
        session.ShareMacro(macroId);

        session.UnshareMacro(macroId);

        Assert.DoesNotContain(macroId, session.SharedMacroIds);
    }

    [Fact]
    public void UnshareMacro_NotShared_DoesNotThrow()
    {
        var session = CreateValid(out _);

        var exception = Record.Exception(() => session.UnshareMacro(Guid.NewGuid()));

        Assert.Null(exception);
    }
}
