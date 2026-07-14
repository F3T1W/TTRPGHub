using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateCharacterFromRulesCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IGameSystemRepository _systemRepository = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entryRepository = Substitute.For<IRuleEntryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private CreateCharacterFromRulesCommandHandler CreateHandler() =>
        new(_characterRepository, _systemRepository, _entryRepository, _currentUser, _unitOfWork, _cache);

    private static readonly GameSystem Pf2eSystem = GameSystem.CreateOfficial("pf2e", "Pathfinder 2e");

    // Human-like ancestry: 2 free boosts, no flaw. Fighter-like class: choice of STR or DEX as key ability.
    private static RuleEntry HumanAncestry() => RuleEntry.Create(
        Pf2eSystem.Id, RuleCategory.Race, "human", "Human", null, null,
        """{"boost_codes":["ANY","ANY"],"hp":8,"speed":25}""", [], false, "Test");

    private static RuleEntry FighterClass() => RuleEntry.Create(
        Pf2eSystem.Id, RuleCategory.Class, "fighter", "Fighter", null, null,
        """{"key_ability_codes":["STR","DEX"],"hp_per_level":10}""", [], false, "Test");

    private static RuleEntry SingleKeyAbilityClass() => RuleEntry.Create(
        Pf2eSystem.Id, RuleCategory.Class, "wizard", "Wizard", null, null,
        """{"key_ability_codes":["INT"],"hp_per_level":6}""", [], false, "Test");

    private CreateCharacterFromRulesCommand BaseCommand(
        List<string>? freeBoosts = null, string? keyAbility = null, string classSlug = "fighter") => new(
        "Aragorn", "pf2e", "human", classSlug, 1,
        Strength: 10, Dexterity: 10, Constitution: 10, Intelligence: 10, Wisdom: 10, Charisma: 10,
        FreeBoostAbilityCodes: freeBoosts, KeyAbilityCode: keyAbility);

    [Fact]
    public async Task Handle_UnknownSystem_ReturnsNotFound()
    {
        _systemRepository.GetBySlugAsync("pf2e").Returns((GameSystem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnknownRace_ReturnsNotFound()
    {
        _systemRepository.GetBySlugAsync("pf2e").Returns(Pf2eSystem);
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Race, "human").Returns((RuleEntry?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnknownClass_ReturnsNotFound()
    {
        _systemRepository.GetBySlugAsync("pf2e").Returns(Pf2eSystem);
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Race, "human").Returns(HumanAncestry());
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Class, "fighter").Returns((RuleEntry?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NoFreeBoostChoiceGiven_AnyBoostsAreSkipped()
    {
        _systemRepository.GetBySlugAsync("pf2e").Returns(Pf2eSystem);
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Race, "human").Returns(HumanAncestry());
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Class, "fighter").Returns(FighterClass());
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(freeBoosts: null, keyAbility: null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value!.Strength);
        Assert.Equal(10, result.Value.Dexterity);
    }

    [Fact]
    public async Task Handle_FreeBoostChoices_AreAppliedInOrder()
    {
        _systemRepository.GetBySlugAsync("pf2e").Returns(Pf2eSystem);
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Race, "human").Returns(HumanAncestry());
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Class, "fighter").Returns(FighterClass());
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        // Two "ANY" boosts on the ancestry -> STR then DEX get +2 each; multi-option key ability
        // (STR or DEX) explicitly chosen as STR -> STR gets a third +2.
        var result = await handler.Handle(BaseCommand(freeBoosts: ["STR", "DEX"], keyAbility: "STR"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value!.Strength);
        Assert.Equal(12, result.Value.Dexterity);
    }

    [Fact]
    public async Task Handle_SingleKeyAbility_AppliesAutomaticallyWithoutChoice()
    {
        _systemRepository.GetBySlugAsync("pf2e").Returns(Pf2eSystem);
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Race, "human").Returns(HumanAncestry());
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Class, "wizard").Returns(SingleKeyAbilityClass());
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(BaseCommand(freeBoosts: null, keyAbility: null, classSlug: "wizard"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(12, result.Value!.Intelligence);
    }

    [Fact]
    public async Task Handle_BoostAbove18_OnlyGrantsPlusOne()
    {
        _systemRepository.GetBySlugAsync("pf2e").Returns(Pf2eSystem);
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Race, "human").Returns(HumanAncestry());
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Class, "fighter").Returns(FighterClass());
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var command = BaseCommand(freeBoosts: ["STR", "DEX"], keyAbility: "STR") with { Strength = 18 };
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        // 18 -> +1 (ANY) -> 19 -> +1 (key ability) -> 20; both boosts above the +18 threshold give +1.
        Assert.Equal(20, result.Value!.Strength);
    }

    [Fact]
    public async Task Handle_PersistsCharacterAndInvalidatesOwnerCache()
    {
        _systemRepository.GetBySlugAsync("pf2e").Returns(Pf2eSystem);
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Race, "human").Returns(HumanAncestry());
        _entryRepository.GetBySlugAsync(Pf2eSystem.Id, RuleCategory.Class, "fighter").Returns(FighterClass());
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var handler = CreateHandler();

        await handler.Handle(BaseCommand(), CancellationToken.None);

        await _characterRepository.Received(1).AddAsync(Arg.Any<Character>(), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"characters:owner:{userId}", Arg.Any<CancellationToken>());
    }
}
