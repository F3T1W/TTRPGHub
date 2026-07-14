using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.GameTable.Commands.MoveTableToken;
using TTRPGHub.Features.GameTable.Commands.RemoveTableToken;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class MoveTableTokenCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private MoveTableTokenCommandHandler CreateHandler() =>
        new(_sessionRepository, _tokenRepository, _unitOfWork, _notifier, _currentUser);

    private (GameSession Session, TableToken Token) SetUp(UserId ownerId)
    {
        var session = GameSession.Create(UserId.New(), "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var token = TableToken.Create(session.Id, Guid.NewGuid(), "Goblin", null, "#f00", 5, 5, ownerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(token);
        return (session, token);
    }

    [Fact]
    public async Task Handle_Owner_MovesToken()
    {
        var ownerId = UserId.New();
        var (_, token) = SetUp(ownerId);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTableTokenCommand(Guid.NewGuid(), token.Id, 12, 8), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, token.X);
        Assert.Equal(8, token.Y);
    }

    [Fact]
    public async Task Handle_Stranger_ReturnsUnauthorized()
    {
        var (_, token) = SetUp(UserId.New());
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTableTokenCommand(Guid.NewGuid(), token.Id, 12, 8), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_TokenFromDifferentSession_ReturnsNotFound()
    {
        var (_, token) = SetUp(UserId.New());
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(
            TableToken.Create(GameSessionId.New(), Guid.NewGuid(), "Other", null, "#000", 0, 0, null));
        var handler = CreateHandler();

        var result = await handler.Handle(new MoveTableTokenCommand(Guid.NewGuid(), token.Id, 1, 1), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NotifiesTableWithNewPosition()
    {
        var ownerId = UserId.New();
        var (_, token) = SetUp(ownerId);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        await handler.Handle(new MoveTableTokenCommand(Guid.NewGuid(), token.Id, 7, 3), CancellationToken.None);

        await _notifier.Received(1).NotifyTokenMovedAsync(Arg.Any<Guid>(), token.Id, 7, 3, Arg.Any<CancellationToken>());
    }
}

public class RemoveTableTokenCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private RemoveTableTokenCommandHandler CreateHandler() =>
        new(_sessionRepository, _tokenRepository, _unitOfWork, _notifier, _currentUser);

    [Fact]
    public async Task Handle_TokenOwnerButNotOrganizer_ReturnsUnauthorized()
    {
        var organizerId = UserId.New();
        var ownerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var token = TableToken.Create(session.Id, Guid.NewGuid(), "Goblin", null, "#f00", 5, 5, ownerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(token);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new RemoveTableTokenCommand(Guid.NewGuid(), token.Id), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Organizer_RemovesTokenAndNotifies()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var token = TableToken.Create(session.Id, Guid.NewGuid(), "Goblin", null, "#f00", 5, 5, UserId.New());
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(token);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new RemoveTableTokenCommand(session.Id.Value, token.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _tokenRepository.Received(1).Remove(token);
        await _notifier.Received(1).NotifyTokenRemovedAsync(session.Id.Value, token.Id, Arg.Any<CancellationToken>());
    }
}
