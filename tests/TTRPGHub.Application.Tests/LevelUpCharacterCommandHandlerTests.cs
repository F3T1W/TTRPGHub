using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Characters.Commands.LevelUpCharacter;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class LevelUpCharacterCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly IGameSystemRepository _systemRepository = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entryRepository = Substitute.For<IRuleEntryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ITableNotifier _notifier = Substitute.For<ITableNotifier>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private LevelUpCharacterCommandHandler CreateHandler() => new(
        _characterRepository, _tokenRepository, _systemRepository, _entryRepository,
        _currentUser, _unitOfWork, _notifier, _cache);

    // 1d10 hit die, Constitution 10 (modifier 0): level 1 = 10 HP, each further level = 5 (d10/2) + 1 = 6.
    private static Character CreateFighter(UserId ownerId, int level = 1)
    {
        var character = Character.Create(ownerId, "Aragorn", "Human", "Fighter", level).Value!;
        character.UpdateSheet(new UpdateSheetData(
            "Aragorn", "Human", "Fighter", level, false, null, null, 0, null, null, null, null,
            10, 10, 10, 10, 10, 10, 10, 10, 0, 10, 30, "1d10", [], [], null, null));
        return character;
    }

    [Fact]
    public async Task Handle_CharacterNotFound_ReturnsNotFound()
    {
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns((Character?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 2), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var character = CreateFighter(UserId.New());
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 2), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_CoOwner_IsAllowed()
    {
        var ownerId = UserId.New();
        var character = CreateFighter(ownerId);
        var coOwnerId = Guid.NewGuid();
        character.AddCoOwner(coOwnerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(new UserId(coOwnerId));
        var handler = CreateHandler();

        var result = await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_SameOrLowerLevel_ReturnsValidationError()
    {
        var ownerId = UserId.New();
        var character = CreateFighter(ownerId, level: 5);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 5), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_AboveMaxLevel_ReturnsValidationError()
    {
        var ownerId = UserId.New();
        var character = CreateFighter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 21), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidLevelUp_RecalculatesMaxHpFromHitDice()
    {
        var ownerId = UserId.New();
        var character = CreateFighter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Level);
        Assert.Equal(16, result.Value.MaxHitPoints);
    }

    [Fact]
    public async Task Handle_ValidLevelUp_GainsCurrentHpByTheSameDelta()
    {
        var ownerId = UserId.New();
        var character = CreateFighter(ownerId);
        character.SetCurrentHitPoints(5);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        // MaxHp went 10 -> 16 (+6), CurrentHp was 5 -> should also gain 6 -> 11.
        Assert.Equal(11, result.Value!.CurrentHitPoints);
    }

    [Fact]
    public async Task Handle_NoLinkedTokens_DoesNotNotifyTable()
    {
        var ownerId = UserId.New();
        var character = CreateFighter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        _tokenRepository.GetByCombatantAsync(TokenCombatantType.Character, Arg.Any<Guid>())
            .Returns((IReadOnlyList<TableToken>)[]);
        var handler = CreateHandler();

        await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 2), CancellationToken.None);

        await _notifier.DidNotReceive().NotifyTokenUpdatedAsync(
            Arg.Any<Guid>(), Arg.Any<Features.GameTable.Shared.TableTokenDto>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LinkedToken_SyncsHpAndArmorClass()
    {
        var ownerId = UserId.New();
        var character = CreateFighter(ownerId);
        var session = GameSession.Create(UserId.New(), "Test", null, "dnd5e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        var token = TableToken.Create(session.Id, Guid.NewGuid(), "Aragorn", null, "#f00", 1, 1, ownerId,
            combatantType: TokenCombatantType.Character, combatantId: character.Id.Value,
            currentHp: 10, maxHp: 10, armorClass: 10);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        _tokenRepository.GetByCombatantAsync(TokenCombatantType.Character, character.Id.Value)
            .Returns((IReadOnlyList<TableToken>)[token]);
        var handler = CreateHandler();

        var result = await handler.Handle(new LevelUpCharacterCommand(Guid.NewGuid(), 2), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(16, token.MaxHp);
        await _notifier.Received(1).NotifyTokenUpdatedAsync(
            session.Id.Value, Arg.Any<Features.GameTable.Shared.TableTokenDto>(), Arg.Any<CancellationToken>());
    }
}
