using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Macros.Commands.UnshareMacro;
using TTRPGHub.Features.Macros.Queries.GetSharedMacros;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class UnshareMacroCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly IMacroRepository _macroRepository = Substitute.For<IMacroRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();

    private UnshareMacroCommandHandler CreateHandler() =>
        new(_sessionRepository, _macroRepository, _unitOfWork, _currentUser, _notifier);

    [Fact]
    public async Task Handle_NotOrganizer_ReturnsUnauthorized()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new UnshareMacroCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Organizer_RemovesIdAndNotifiesWithRemainder()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var macroId = Guid.NewGuid();
        session.ShareMacro(macroId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        _macroRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns([]);
        var handler = CreateHandler();

        var result = await handler.Handle(new UnshareMacroCommand(Guid.NewGuid(), macroId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(macroId, session.SharedMacroIds);
        await _notifier.Received(1).NotifySharedMacrosChangedAsync(
            Arg.Any<Guid>(), Arg.Is<List<Features.Macros.Shared.MacroDto>>(l => l.Count == 0), Arg.Any<CancellationToken>());
    }
}

public class GetSharedMacrosQueryHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly IMacroRepository _macroRepository = Substitute.For<IMacroRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetSharedMacrosQueryHandler CreateHandler() =>
        new(_sessionRepository, _macroRepository, _currentUser);

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsNotFound()
    {
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns((GameSession?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSharedMacrosQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Stranger_ReturnsUnauthorized()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSharedMacrosQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Participant_ReturnsSharedMacros()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var playerId = UserId.New();
        session.Join(playerId);
        var macro = Macro.Create(organizerId, "Heal", null, MacroType.Chat, "/r 1d8");
        session.ShareMacro(macro.Id);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(playerId);
        _macroRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns([macro]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSharedMacrosQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}
