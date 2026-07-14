using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Features.GameTable.Commands.ApplyTokenCondition;
using TTRPGHub.Features.GameTable.Commands.RemoveTokenCondition;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Application.Tests;

public class ApplyTokenConditionCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly IPf2eMonsterRepository _monsterRepository = Substitute.For<IPf2eMonsterRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private ApplyTokenConditionCommandHandler CreateHandler() => new(
        _sessionRepository, _tokenRepository, _monsterRepository, _unitOfWork, _notifier, _currentUser,
        TestDoubles.CreateInertTrackerSync());

    private (GameSession Session, TableToken Token) SetUp(UserId ownerId, Guid? monsterCombatantId = null)
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var token = monsterCombatantId is { } monsterId
            ? TableToken.Create(session.Id, Guid.NewGuid(), "Ogre", null, "#f00", 1, 1, ownerId,
                combatantType: TokenCombatantType.Pf2eMonster, combatantId: monsterId)
            : TableToken.Create(session.Id, Guid.NewGuid(), "Ogre", null, "#f00", 1, 1, ownerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(token);
        _currentUser.Id.Returns(ownerId);
        return (session, token);
    }

    [Fact]
    public async Task Handle_BlankSlug_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ApplyTokenConditionCommand(Guid.NewGuid(), Guid.NewGuid(), "", "Frightened", 1), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidCondition_AppliesAndSaves()
    {
        var ownerId = UserId.New();
        var (_, token) = SetUp(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ApplyTokenConditionCommand(Guid.NewGuid(), token.Id, "frightened", "Frightened", 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(token.Conditions, c => c.Slug == "frightened" && c.Value == 1);
    }

    [Fact]
    public async Task Handle_MonsterImmuneToCondition_Blocks()
    {
        var ownerId = UserId.New();
        var monsterId = Guid.NewGuid();
        var (_, token) = SetUp(ownerId, monsterId);
        var monster = Pf2eMonster.Create(
            "fire-elemental", "Fire Elemental", 5, "Large", "elemental fire",
            10, null, null, null, 4, 2, 3, 0, 1, -1,
            20, 12, 8, 6, 80, "40 feet", null, null, "Test",
            immunitiesJson: """[{"type":"frightened"}]""");
        _monsterRepository.GetByIdAsync(Arg.Any<Pf2eMonsterId>()).Returns(monster);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ApplyTokenConditionCommand(Guid.NewGuid(), token.Id, "frightened", "Frightened", 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(token.Conditions);
    }

    [Fact]
    public async Task Handle_MonsterImmuneToDifferentCondition_StillApplies()
    {
        var ownerId = UserId.New();
        var monsterId = Guid.NewGuid();
        var (_, token) = SetUp(ownerId, monsterId);
        var monster = Pf2eMonster.Create(
            "fire-elemental", "Fire Elemental", 5, "Large", "elemental fire",
            10, null, null, null, 4, 2, 3, 0, 1, -1,
            20, 12, 8, 6, 80, "40 feet", null, null, "Test",
            immunitiesJson: """[{"type":"fire"}]""");
        _monsterRepository.GetByIdAsync(Arg.Any<Pf2eMonsterId>()).Returns(monster);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ApplyTokenConditionCommand(Guid.NewGuid(), token.Id, "frightened", "Frightened", 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(token.Conditions, c => c.Slug == "frightened");
    }

    [Fact]
    public async Task Handle_Stranger_ReturnsUnauthorized()
    {
        var (_, token) = SetUp(UserId.New());
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ApplyTokenConditionCommand(Guid.NewGuid(), token.Id, "frightened", "Frightened", 1), CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}

public class RemoveTokenConditionCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private RemoveTokenConditionCommandHandler CreateHandler() => new(
        _sessionRepository, _tokenRepository, _unitOfWork, _notifier, _currentUser,
        TestDoubles.CreateInertTrackerSync());

    [Fact]
    public async Task Handle_RemovesConditionBySlug()
    {
        var ownerId = UserId.New();
        var session = GameSession.Create(UserId.New(), "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var token = TableToken.Create(session.Id, Guid.NewGuid(), "Ogre", null, "#f00", 1, 1, ownerId);
        token.ApplyCondition("prone", "Prone", null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(token);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new RemoveTokenConditionCommand(Guid.NewGuid(), token.Id, "prone"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(token.Conditions);
    }

    [Fact]
    public async Task Handle_Stranger_ReturnsUnauthorized()
    {
        var session = GameSession.Create(UserId.New(), "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var token = TableToken.Create(session.Id, Guid.NewGuid(), "Ogre", null, "#f00", 1, 1, UserId.New());
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _tokenRepository.GetByIdAsync(Arg.Any<Guid>()).Returns(token);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new RemoveTokenConditionCommand(Guid.NewGuid(), token.Id, "prone"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}
