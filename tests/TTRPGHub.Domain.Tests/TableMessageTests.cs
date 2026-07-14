using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class TableMessageTests
{
    [Fact]
    public void IsVisibleTo_ChatMessage_VisibleToAnyone()
    {
        var message = TableMessage.CreateChat(GameSessionId.New(), UserId.New(), "GM", "Hello");

        Assert.True(message.IsVisibleTo(UserId.New()));
    }

    [Fact]
    public void IsVisibleTo_RollMessage_VisibleToAnyone()
    {
        var message = TableMessage.CreateRoll(GameSessionId.New(), UserId.New(), "GM", "rolled a 20");

        Assert.True(message.IsVisibleTo(UserId.New()));
    }

    [Fact]
    public void IsVisibleTo_SystemMessage_VisibleToAnyone()
    {
        var message = TableMessage.CreateSystem(GameSessionId.New(), "Combat started");

        Assert.True(message.IsVisibleTo(UserId.New()));
    }

    [Fact]
    public void IsVisibleTo_Whisper_VisibleToSender()
    {
        var senderId = UserId.New();
        var message = TableMessage.CreateWhisper(GameSessionId.New(), senderId, "GM", UserId.New(), "Player", "psst");

        Assert.True(message.IsVisibleTo(senderId));
    }

    [Fact]
    public void IsVisibleTo_Whisper_VisibleToRecipient()
    {
        var recipientId = UserId.New();
        var message = TableMessage.CreateWhisper(GameSessionId.New(), UserId.New(), "GM", recipientId, "Player", "psst");

        Assert.True(message.IsVisibleTo(recipientId));
    }

    [Fact]
    public void IsVisibleTo_Whisper_HiddenFromUninvolvedThirdParty()
    {
        var message = TableMessage.CreateWhisper(GameSessionId.New(), UserId.New(), "GM", UserId.New(), "Player", "psst");

        Assert.False(message.IsVisibleTo(UserId.New()));
    }
}
