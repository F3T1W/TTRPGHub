using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Features.Characters.Commands.CreateCharacter;
using TTRPGHub.Features.Characters.Commands.CreateChronicle;
using TTRPGHub.Features.Characters.Commands.CreateCompanion;
using TTRPGHub.Features.Characters.Commands.DeleteChronicle;
using TTRPGHub.Features.Characters.Commands.DeleteCompanion;
using TTRPGHub.Features.Characters.Commands.UpdateCharacterFeats;
using TTRPGHub.Features.Characters.Commands.UpdateCharacterPf2eStats;
using TTRPGHub.Features.Characters.Commands.UpdateCompanion;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Application.Tests;

public class CreateCharacterCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private CreateCharacterCommandHandler CreateHandler() =>
        new(_characterRepository, _currentUser, _unitOfWork, _cache);

    [Fact]
    public async Task Handle_BlankName_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new CreateCharacterCommand("  ", "Human", "Fighter", 1), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _characterRepository.DidNotReceive().AddAsync(Arg.Any<Character>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesCharacterOwnedByCurrentUserAndInvalidatesCache()
    {
        var ownerId = UserId.New();
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new CreateCharacterCommand("Grog", "Human", "Fighter", 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _characterRepository.Received(1).AddAsync(
            Arg.Is<Character>(c => c.OwnerId == ownerId && c.Name == "Grog"), Arg.Any<CancellationToken>());
        await _cache.Received(1).RemoveAsync($"characters:owner:{ownerId}", Arg.Any<CancellationToken>());
    }
}

public class UpdateCharacterFeatsCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private UpdateCharacterFeatsCommandHandler CreateHandler() =>
        new(_characterRepository, _currentUser, _unitOfWork, _cache);

    private static Character CreateCharacter(UserId ownerId) =>
        Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;

    [Fact]
    public async Task Handle_CharacterNotFound_ReturnsNotFound()
    {
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns((Character?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateCharacterFeatsCommand(Guid.NewGuid(), "[]"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonOwner_ReturnsUnauthorized()
    {
        var character = CreateCharacter(UserId.New());
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateCharacterFeatsCommand(character.Id.Value, "[]"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsValidationError()
    {
        var ownerId = UserId.New();
        var character = CreateCharacter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateCharacterFeatsCommand(character.Id.Value, "{not json"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidJson_UpdatesFeatsAndInvalidatesCache()
    {
        var ownerId = UserId.New();
        var character = CreateCharacter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCharacterFeatsCommand(character.Id.Value, """["power-attack"]"""), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _cache.Received(1).RemoveAsync($"characters:{character.Id.Value}", Arg.Any<CancellationToken>());
    }
}

public class UpdateCharacterPf2eStatsCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private UpdateCharacterPf2eStatsCommandHandler CreateHandler() =>
        new(_characterRepository, _currentUser, _unitOfWork, _cache);

    private static Character CreateCharacter(UserId ownerId) =>
        Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;

    [Fact]
    public async Task Handle_NonOwner_ReturnsUnauthorized()
    {
        var character = CreateCharacter(UserId.New());
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateCharacterPf2eStatsCommand(character.Id.Value, "{}"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_InvalidJson_ReturnsValidationError()
    {
        var ownerId = UserId.New();
        var character = CreateCharacter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateCharacterPf2eStatsCommand(character.Id.Value, "not-json"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_CoOwner_CanUpdateStats()
    {
        var ownerId = UserId.New();
        var coOwnerId = UserId.New();
        var character = CreateCharacter(ownerId);
        character.AddCoOwner(coOwnerId.Value);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(coOwnerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCharacterPf2eStatsCommand(character.Id.Value, """{"ac":18}"""), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

public class CreateCompanionCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICompanionRepository _companionRepository = Substitute.For<ICompanionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateCompanionCommandHandler CreateHandler() =>
        new(_characterRepository, _companionRepository, _currentUser, _unitOfWork);

    private static Character CreateCharacter(UserId ownerId) =>
        Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;

    [Fact]
    public async Task Handle_CharacterNotFound_ReturnsNotFound()
    {
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns((Character?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateCompanionCommand(Guid.NewGuid(), "Wolf", "Animal Companion", 1, 10, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonOwner_ReturnsUnauthorized()
    {
        var character = CreateCharacter(UserId.New());
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateCompanionCommand(character.Id.Value, "Wolf", "Animal Companion", 1, 10, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_BlankName_ReturnsValidationError()
    {
        var ownerId = UserId.New();
        var character = CreateCharacter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateCompanionCommand(character.Id.Value, "  ", "Animal Companion", 1, 10, null, null, null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Owner_CreatesCompanionForCharacter()
    {
        var ownerId = UserId.New();
        var character = CreateCharacter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateCompanionCommand(character.Id.Value, "Wolf", "Animal Companion", 1, 10, 16, "30 ft", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _companionRepository.Received(1).AddAsync(
            Arg.Is<Companion>(c => c.OwnerCharacterId == character.Id && c.Name == "Wolf"), Arg.Any<CancellationToken>());
    }
}

public class UpdateCompanionCommandHandlerTests
{
    private readonly ICompanionRepository _companionRepository = Substitute.For<ICompanionRepository>();
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateCompanionCommandHandler CreateHandler() =>
        new(_companionRepository, _characterRepository, _currentUser, _unitOfWork);

    private static (Character Character, Companion Companion) CreateOwnedCompanion(UserId ownerId)
    {
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;
        var companion = Companion.Create(character.Id, "Wolf", "Animal Companion", 1, 10, 16, "30 ft", null, null, null).Value!;
        return (character, companion);
    }

    [Fact]
    public async Task Handle_CompanionNotFound_ReturnsNotFound()
    {
        _companionRepository.GetByIdAsync(Arg.Any<CompanionId>()).Returns((Companion?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCompanionCommand(Guid.NewGuid(), "Wolf", "Animal Companion", 2, 14, 14, 16, "30 ft", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonOwnerOfParentCharacter_ReturnsUnauthorized()
    {
        var (character, companion) = CreateOwnedCompanion(UserId.New());
        _companionRepository.GetByIdAsync(Arg.Any<CompanionId>()).Returns(companion);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCompanionCommand(companion.Id.Value, "Wolf", "Animal Companion", 2, 14, 14, 16, "30 ft", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Owner_UpdatesCompanion()
    {
        var ownerId = UserId.New();
        var (character, companion) = CreateOwnedCompanion(ownerId);
        _companionRepository.GetByIdAsync(Arg.Any<CompanionId>()).Returns(companion);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCompanionCommand(companion.Id.Value, "Direwolf", "Animal Companion", 2, 14, 14, 17, "40 ft", null, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Direwolf", companion.Name);
        Assert.Equal(2, companion.Level);
        _companionRepository.Received(1).Update(companion);
    }
}

public class DeleteCompanionCommandHandlerTests
{
    private readonly ICompanionRepository _companionRepository = Substitute.For<ICompanionRepository>();
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteCompanionCommandHandler CreateHandler() =>
        new(_companionRepository, _characterRepository, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_NonOwnerOfParentCharacter_ReturnsUnauthorized()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 1).Value!;
        var companion = Companion.Create(character.Id, "Wolf", "Animal Companion", 1, 10, null, null, null, null, null).Value!;
        _companionRepository.GetByIdAsync(Arg.Any<CompanionId>()).Returns(companion);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteCompanionCommand(companion.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        _companionRepository.DidNotReceive().Delete(Arg.Any<Companion>());
    }

    [Fact]
    public async Task Handle_Owner_DeletesCompanion()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;
        var companion = Companion.Create(character.Id, "Wolf", "Animal Companion", 1, 10, null, null, null, null, null).Value!;
        _companionRepository.GetByIdAsync(Arg.Any<CompanionId>()).Returns(companion);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteCompanionCommand(companion.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _companionRepository.Received(1).Delete(companion);
    }
}

public class CreateChronicleCommandHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IPathfinderSocietyChronicleRepository _chronicleRepository = Substitute.For<IPathfinderSocietyChronicleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateChronicleCommandHandler CreateHandler() =>
        new(_characterRepository, _chronicleRepository, _currentUser, _unitOfWork);

    private static Character CreateCharacter(UserId ownerId) =>
        Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;

    [Fact]
    public async Task Handle_NonOwner_ReturnsUnauthorized()
    {
        var character = CreateCharacter(UserId.New());
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateChronicleCommand(character.Id.Value, "Scenario 1-01", DateOnly.FromDateTime(DateTime.Today), null, null, 0, 0, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_BlankScenarioName_ReturnsValidationError()
    {
        var ownerId = UserId.New();
        var character = CreateCharacter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateChronicleCommand(character.Id.Value, "  ", DateOnly.FromDateTime(DateTime.Today), null, null, 0, 0, null, null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Owner_CreatesChronicleForCharacter()
    {
        var ownerId = UserId.New();
        var character = CreateCharacter(ownerId);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateChronicleCommand(character.Id.Value, "Scenario 1-01", DateOnly.FromDateTime(DateTime.Today), "GM Dave", "Grand Lodge", 12, 4, null, null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _chronicleRepository.Received(1).AddAsync(
            Arg.Is<PathfinderSocietyChronicle>(c => c.CharacterId == character.Id), Arg.Any<CancellationToken>());
    }
}

public class DeleteChronicleCommandHandlerTests
{
    private readonly IPathfinderSocietyChronicleRepository _chronicleRepository = Substitute.For<IPathfinderSocietyChronicleRepository>();
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteChronicleCommandHandler CreateHandler() =>
        new(_chronicleRepository, _characterRepository, _currentUser, _unitOfWork);

    private static (Character Character, PathfinderSocietyChronicle Chronicle) CreateOwnedChronicle(UserId ownerId)
    {
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;
        var chronicle = PathfinderSocietyChronicle.Create(
            character.Id, "Scenario 1-01", DateOnly.FromDateTime(DateTime.Today), null, null, 0, 0, null, null).Value!;
        return (character, chronicle);
    }

    [Fact]
    public async Task Handle_NonOwnerOfParentCharacter_ReturnsUnauthorized()
    {
        var (character, chronicle) = CreateOwnedChronicle(UserId.New());
        _chronicleRepository.GetByIdAsync(Arg.Any<PathfinderSocietyChronicleId>()).Returns(chronicle);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteChronicleCommand(chronicle.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        _chronicleRepository.DidNotReceive().Delete(Arg.Any<PathfinderSocietyChronicle>());
    }

    [Fact]
    public async Task Handle_Owner_DeletesChronicle()
    {
        var ownerId = UserId.New();
        var (character, chronicle) = CreateOwnedChronicle(ownerId);
        _chronicleRepository.GetByIdAsync(Arg.Any<PathfinderSocietyChronicleId>()).Returns(chronicle);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteChronicleCommand(chronicle.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _chronicleRepository.Received(1).Delete(chronicle);
    }
}
