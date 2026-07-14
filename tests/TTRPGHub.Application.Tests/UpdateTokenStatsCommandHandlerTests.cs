using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Commands.UpdateTokenStats;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class UpdateTokenStatsCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private UpdateTokenStatsCommandHandler CreateHandler() => new(
        _sessionRepository, _tokenRepository, _characterRepository, _unitOfWork, _notifier, _currentUser,
        TestDoubles.CreateInertTrackerSync());

    private (GameSession Session, TableToken Token) CreateSessionWithOwnedToken(UserId ownerId, out UserId organizerId)
    {
        organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var token = TableToken.Create(session.Id, Guid.NewGuid(), "Goblin", null, "#f00", 1, 1, ownerId,
            currentHp: 10, maxHp: 10, armorClass: 15);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(token);
        return (session, token);
    }

    [Fact]
    public async Task Handle_TokenBelongsToDifferentSession_ReturnsNotFound()
    {
        var (_, token) = CreateSessionWithOwnedToken(UserId.New(), out _);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(
            TableToken.Create(GameSessionId.New(), Guid.NewGuid(), "Other", null, "#000", 0, 0, null));
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, 5, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_StrangerCannotMoveToken_ReturnsUnauthorized()
    {
        var (_, token) = CreateSessionWithOwnedToken(UserId.New(), out _);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, 5, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OwnerCanUpdateHp()
    {
        var ownerId = UserId.New();
        var (_, token) = CreateSessionWithOwnedToken(ownerId, out _);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, 3, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, token.CurrentHp);
    }

    [Fact]
    public async Task Handle_LinkedCharacterHp_SyncsToCharacter()
    {
        var ownerId = UserId.New();
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var character = Character.Create(ownerId, "Aragorn", "Human", "Fighter", 5).Value!;
        character.UpdateSheet(new UpdateSheetData(
            "Aragorn", "Human", "Fighter", 5, false, null, null, 0, null, null, null, null,
            10, 10, 10, 10, 10, 10, 30, 30, 0, 10, 30, "1d10", [], [], null, null));
        var token = TableToken.Create(session.Id, Guid.NewGuid(), "Aragorn", null, "#f00", 1, 1, ownerId,
            combatantType: TokenCombatantType.Character, combatantId: character.Id.Value,
            currentHp: 30, maxHp: 30, armorClass: 18);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(token);
        _characterRepository.GetByIdAsync(character.Id).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, 12, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, character.CurrentHitPoints);
    }

    [Fact]
    public async Task Handle_NonOrganizerResizingToken_ReturnsUnauthorized()
    {
        var ownerId = UserId.New();
        var (_, token) = CreateSessionWithOwnedToken(ownerId, out _);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, null, 2, 2, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OrganizerCanResizeToken()
    {
        var (_, token) = CreateSessionWithOwnedToken(UserId.New(), out var organizerId);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, null, 2, 2, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, token.Width);
    }

    [Fact]
    public async Task Handle_NonOrganizerAddingCoOwner_ReturnsUnauthorized()
    {
        var ownerId = UserId.New();
        var (_, token) = CreateSessionWithOwnedToken(ownerId, out _);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var command = new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, null, null, null, null, AddCoOwnerId: Guid.NewGuid());
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OrganizerAddingCoOwner_Succeeds()
    {
        var (_, token) = CreateSessionWithOwnedToken(UserId.New(), out var organizerId);
        _currentUser.Id.Returns(organizerId);
        var coOwnerId = Guid.NewGuid();
        var handler = CreateHandler();

        var command = new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, null, null, null, null, AddCoOwnerId: coOwnerId);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(token.IsOwnedBy(new UserId(coOwnerId)));
    }

    [Fact]
    public async Task Handle_RotationAllowedForOwnerNotJustOrganizer()
    {
        var ownerId = UserId.New();
        var (_, token) = CreateSessionWithOwnedToken(ownerId, out _);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateTokenStatsCommand(Guid.NewGuid(), token.Id, null, null, null, -45), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(315, token.Rotation);
    }
}
