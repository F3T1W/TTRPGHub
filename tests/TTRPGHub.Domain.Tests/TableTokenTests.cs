using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class TableTokenTests
{
    private static TableToken CreateOwned(out UserId ownerId)
    {
        ownerId = UserId.New();
        return TableToken.Create(GameSessionId.New(), Guid.NewGuid(), "Goblin", null, "#ff0000", 5, 5, ownerId);
    }

    [Fact]
    public void CanBeMovedBy_Organizer_AlwaysTrue()
    {
        var token = CreateOwned(out _);

        Assert.True(token.CanBeMovedBy(UserId.New(), isOrganizer: true));
    }

    [Fact]
    public void CanBeMovedBy_Owner_True()
    {
        var token = CreateOwned(out var ownerId);

        Assert.True(token.CanBeMovedBy(ownerId, isOrganizer: false));
    }

    [Fact]
    public void CanBeMovedBy_Stranger_False()
    {
        var token = CreateOwned(out _);

        Assert.False(token.CanBeMovedBy(UserId.New(), isOrganizer: false));
    }

    [Fact]
    public void CanBeMovedBy_CoOwner_TrueAfterAdd()
    {
        var token = CreateOwned(out _);
        var coOwnerId = Guid.NewGuid();
        token.AddCoOwner(coOwnerId);

        Assert.True(token.CanBeMovedBy(new UserId(coOwnerId), isOrganizer: false));
    }

    [Fact]
    public void AddCoOwner_AlreadyOwner_Fails()
    {
        var token = CreateOwned(out var ownerId);

        var result = token.AddCoOwner(ownerId.Value);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void AddCoOwner_Duplicate_Fails()
    {
        var token = CreateOwned(out _);
        var coOwnerId = Guid.NewGuid();
        token.AddCoOwner(coOwnerId);

        var result = token.AddCoOwner(coOwnerId);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RemoveCoOwner_RevokesMovePermission()
    {
        var token = CreateOwned(out _);
        var coOwnerId = Guid.NewGuid();
        token.AddCoOwner(coOwnerId);

        token.RemoveCoOwner(coOwnerId);

        Assert.False(token.CanBeMovedBy(new UserId(coOwnerId), isOrganizer: false));
    }

    [Fact]
    public void IsVisibleTo_NullList_VisibleToEveryone()
    {
        var token = CreateOwned(out _);

        Assert.True(token.IsVisibleTo(UserId.New(), isOrganizer: false, visibleToUserIds: null));
    }

    [Fact]
    public void IsVisibleTo_EmptyList_HiddenFromStranger()
    {
        var token = CreateOwned(out _);

        Assert.False(token.IsVisibleTo(UserId.New(), isOrganizer: false, visibleToUserIds: []));
    }

    [Fact]
    public void IsVisibleTo_EmptyList_StillVisibleToOrganizer()
    {
        var token = CreateOwned(out _);

        Assert.True(token.IsVisibleTo(UserId.New(), isOrganizer: true, visibleToUserIds: []));
    }

    [Fact]
    public void IsVisibleTo_EmptyList_StillVisibleToOwner()
    {
        var token = CreateOwned(out var ownerId);

        Assert.True(token.IsVisibleTo(ownerId, isOrganizer: false, visibleToUserIds: []));
    }

    [Fact]
    public void IsVisibleTo_ExplicitList_OnlyListedPlayersSeeIt()
    {
        var token = CreateOwned(out _);
        var allowedId = Guid.NewGuid();

        Assert.True(token.IsVisibleTo(new UserId(allowedId), isOrganizer: false, visibleToUserIds: [allowedId]));
        Assert.False(token.IsVisibleTo(UserId.New(), isOrganizer: false, visibleToUserIds: [allowedId]));
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(250, 200)]
    public void Move_ClampsCoordinatesTo0Through200(double input, double expected)
    {
        var token = CreateOwned(out _);

        token.Move(input, input);

        Assert.Equal(expected, token.X);
        Assert.Equal(expected, token.Y);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(10, 6)]
    public void Resize_ClampsSizeTo1Through6(int input, int expected)
    {
        var token = CreateOwned(out _);

        token.Resize(input, input);

        Assert.Equal(expected, token.Width);
        Assert.Equal(expected, token.Height);
    }

    [Fact]
    public void ApplyCondition_SameSlugTwice_ReplacesNotDuplicates()
    {
        var token = CreateOwned(out _);

        token.ApplyCondition("frightened", "Frightened", 1);
        token.ApplyCondition("frightened", "Frightened", 2);

        var condition = Assert.Single(token.Conditions);
        Assert.Equal(2, condition.Value);
    }

    [Fact]
    public void RemoveCondition_RemovesBySlug()
    {
        var token = CreateOwned(out _);
        token.ApplyCondition("prone", "Prone", null);

        token.RemoveCondition("prone");

        Assert.Empty(token.Conditions);
    }

    [Fact]
    public void Rotate_NormalizesInto0To359Range()
    {
        var token = CreateOwned(out _);

        token.Rotate(-45);

        Assert.Equal(315, token.Rotation);
    }
}
