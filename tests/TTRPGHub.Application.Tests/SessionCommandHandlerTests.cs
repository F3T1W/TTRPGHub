using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Sessions.Commands.CreateSession;
using TTRPGHub.Features.Sessions.Commands.JoinSession;
using TTRPGHub.Features.Sessions.Commands.LeaveSession;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateSessionCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ISceneRepository _sceneRepository = Substitute.For<ISceneRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private CreateSessionCommandHandler CreateHandler() =>
        new(_sessionRepository, _sceneRepository, _unitOfWork, _currentUser);

    private static CreateSessionCommand ValidCommand() => new(
        "Curse of the Crimson Throne", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);

    [Fact]
    public async Task Handle_CreatesSessionOwnedByCurrentUser()
    {
        var organizerId = UserId.New();
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _sessionRepository.Received(1).AddAsync(
            Arg.Is<GameSession>(s => s.OrganizerId == organizerId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesDefaultSceneAndActivatesIt()
    {
        var organizerId = UserId.New();
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        await handler.Handle(ValidCommand(), CancellationToken.None);

        await _sceneRepository.Received(1).AddAsync(
            Arg.Is<Scene>(s => s.Name == "Сцена 1"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ConvertsScheduledAtToUtc()
    {
        var organizerId = UserId.New();
        _currentUser.Id.Returns(organizerId);
        GameSession? captured = null;
        _sessionRepository.When(r => r.AddAsync(Arg.Any<GameSession>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<GameSession>());
        var handler = CreateHandler();
        var localTime = DateTime.SpecifyKind(DateTime.UtcNow.AddDays(2), DateTimeKind.Local);

        await handler.Handle(ValidCommand() with { ScheduledAt = localTime }, CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, captured!.ScheduledAt.Kind);
    }
}

public class JoinSessionCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private JoinSessionCommandHandler CreateHandler() => new(_sessionRepository, _unitOfWork, _currentUser);

    private static GameSession CreateOpenSession(out UserId organizerId, int maxPlayers = 4)
    {
        organizerId = UserId.New();
        return GameSession.Create(organizerId, "Test", null, "pf2e", maxPlayers, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
    }

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsNotFound()
    {
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns((GameSession?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new JoinSessionCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidJoin_AddsParticipantAndSaves()
    {
        var session = CreateOpenSession(out _);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        var playerId = UserId.New();
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new JoinSessionCommand(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(session.IsParticipant(playerId));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SessionFull_ReturnsValidationError()
    {
        var session = CreateOpenSession(out _, maxPlayers: 1);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new JoinSessionCommand(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}

public class LeaveSessionCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private LeaveSessionCommandHandler CreateHandler() => new(_sessionRepository, _unitOfWork, _currentUser);

    [Fact]
    public async Task Handle_Organizer_CannotLeaveOwnSession()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new LeaveSessionCommand(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.True(session.IsParticipant(organizerId));
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsValidationError()
    {
        var session = GameSession.Create(UserId.New(), "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new LeaveSessionCommand(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Player_LeavesSuccessfully()
    {
        var session = GameSession.Create(UserId.New(), "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var playerId = UserId.New();
        session.Join(playerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new LeaveSessionCommand(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(session.IsParticipant(playerId));
    }
}
