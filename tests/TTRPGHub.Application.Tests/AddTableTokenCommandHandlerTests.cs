using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Features.GameTable.Commands.AddTableToken;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Dnd5e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Application.Tests;

public class AddTableTokenCommandHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IPf2eMonsterRepository _pf2eMonsterRepository = Substitute.For<IPf2eMonsterRepository>();
    private readonly IPf2eHazardRepository _pf2eHazardRepository = Substitute.For<IPf2eHazardRepository>();
    private readonly IDnd5eMonsterRepository _dnd5eMonsterRepository = Substitute.For<IDnd5eMonsterRepository>();
    private readonly ICompanionRepository _companionRepository = Substitute.For<ICompanionRepository>();
    private readonly IPf2eVehicleRepository _pf2eVehicleRepository = Substitute.For<IPf2eVehicleRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AddTableTokenCommandHandler CreateHandler() => new(
        _sessionRepository, _tokenRepository, _characterRepository, _pf2eMonsterRepository,
        _pf2eHazardRepository, _dnd5eMonsterRepository, _companionRepository, _pf2eVehicleRepository,
        _unitOfWork, _notifier, _currentUser);

    private GameSession CreateSessionWithScene(UserId organizerId, out Guid sceneId)
    {
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        sceneId = Guid.NewGuid();
        session.SetActiveScene(organizerId, sceneId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        return session;
    }

    private AddTableTokenCommand BaseCommand(
        Guid sessionId, string combatantType = "None", Guid? combatantId = null, Guid? ownerUserId = null) =>
        new(sessionId, "Goblin", null, "#ff0000", 5, 5, ownerUserId, CombatantType: combatantType, CombatantId: combatantId);

    [Fact]
    public async Task Handle_BlankLabel_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(session.Id.Value) with { Label = "  " }, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnknownCombatantType_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(session.Id.Value, combatantType: "NotARealType"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonOrganizer_ReturnsUnauthorized()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NoActiveScene_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OwnerNotParticipant_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(session.Id.Value, ownerUserId: Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OwnerIsParticipant_Succeeds()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var playerId = UserId.New();
        session.Join(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(session.Id.Value, ownerUserId: playerId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_CombatantTypeWithoutCombatantId_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(session.Id.Value, combatantType: "Pf2eMonster", combatantId: null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_CharacterCombatant_CopiesLabelAndStatsFromCharacter()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var character = Character.Create(UserId.New(), "Aragorn", "Human", "Fighter", 5).Value!;
        character.UpdateSheet(new UpdateSheetData(
            "Aragorn", "Human", "Fighter", 5, false, null, null, 0, null, null, null, null,
            10, 10, 10, 10, 10, 10, 40, 40, 0, 18, 30, "1d10", [], [], null, null));
        _characterRepository.GetByIdAsync(character.Id).Returns(character);
        var handler = CreateHandler();

        var result = await handler.Handle(
            BaseCommand(session.Id.Value, combatantType: "Character", combatantId: character.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var dto = result.Value!;
        Assert.Equal("Aragorn", dto.Label);
        Assert.Equal(40, dto.CurrentHp);
        Assert.Equal(18, dto.ArmorClass);
    }

    [Fact]
    public async Task Handle_UnknownCharacterId_ReturnsNotFound()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns((Character?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            BaseCommand(session.Id.Value, combatantType: "Character", combatantId: Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Pf2eMonsterCombatant_CopiesHpAndAc()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var monster = Pf2eMonster.Create(
            "ogre", "Ogre", 3, "Large", "giant humanoid",
            9, null, null, null, 4, 0, 3, -1, 0, -1,
            17, 11, 5, 6, 30, "25 feet", null, null, "Test");
        _pf2eMonsterRepository.GetByIdAsync(monster.Id).Returns(monster);
        var handler = CreateHandler();

        var result = await handler.Handle(
            BaseCommand(session.Id.Value, combatantType: "Pf2eMonster", combatantId: monster.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Ogre", result.Value!.Label);
        Assert.Equal(30, result.Value.CurrentHp);
        Assert.Equal(30, result.Value.MaxHp);
        Assert.Equal(17, result.Value.ArmorClass);
    }

    [Fact]
    public async Task Handle_HazardWithoutHitPoints_LeavesHpNull()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var hazard = Pf2eHazard.Create(
            "trap", "Pit Trap", "Яма-ловушка", 1, "trap",
            15, null, null, null,
            null, null, null, null, hitPoints: null,
            null, null, null, "Test");
        _pf2eHazardRepository.GetByIdAsync(hazard.Id).Returns(hazard);
        var handler = CreateHandler();

        var result = await handler.Handle(
            BaseCommand(session.Id.Value, combatantType: "Pf2eHazard", combatantId: hazard.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.CurrentHp);
        Assert.Null(result.Value.MaxHp);
    }

    [Fact]
    public async Task Handle_ValidToken_UsesActiveSceneAndNotifiesTable()
    {
        var organizerId = UserId.New();
        var session = CreateSessionWithScene(organizerId, out _);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _tokenRepository.Received(1).AddAsync(Arg.Any<TableToken>(), Arg.Any<CancellationToken>());
        await _notifier.Received(1).NotifyTokenAddedAsync(session.Id.Value, Arg.Any<Features.GameTable.Shared.TableTokenDto>(), Arg.Any<CancellationToken>());
    }
}
