using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Macros.Commands.ShareMacro;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class ShareMacroCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly IMacroRepository _macroRepository = Substitute.For<IMacroRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();

    private ShareMacroCommandHandler CreateHandler() =>
        new(_sessionRepository, _macroRepository, _unitOfWork, _currentUser, _notifier);

    private static GameSession CreateSession(UserId organizerId) =>
        GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsNotFound()
    {
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns((GameSession?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new ShareMacroCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NotOrganizer_ReturnsUnauthorized()
    {
        var organizerId = UserId.New();
        var session = CreateSession(organizerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new ShareMacroCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MacroNotFound_ReturnsNotFound()
    {
        var organizerId = UserId.New();
        var session = CreateSession(organizerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        _macroRepository.GetByIdAsync(Arg.Any<Guid>()).Returns((Macro?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new ShareMacroCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MacroOwnedByAnotherUser_ReturnsUnauthorized()
    {
        var organizerId = UserId.New();
        var session = CreateSession(organizerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        var macro = Macro.Create(UserId.New(), "Heal", null, MacroType.Chat, "/r 1d8");
        _macroRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(macro);
        var handler = CreateHandler();

        var result = await handler.Handle(new ShareMacroCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsToSharedIdsAndNotifies()
    {
        var organizerId = UserId.New();
        var session = CreateSession(organizerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        var macro = Macro.Create(organizerId, "Heal", null, MacroType.Chat, "/r 1d8");
        _macroRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(macro);
        _macroRepository.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>()).Returns([macro]);
        var handler = CreateHandler();

        var result = await handler.Handle(new ShareMacroCommand(Guid.NewGuid(), macro.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(macro.Id, session.SharedMacroIds);
        await _notifier.Received(1).NotifySharedMacrosChangedAsync(
            Arg.Any<Guid>(), Arg.Is<List<Features.Macros.Shared.MacroDto>>(l => l.Count == 1), Arg.Any<CancellationToken>());
    }
}
