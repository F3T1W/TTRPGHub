using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Rules.Commands.CreateGameSystem;
using TTRPGHub.Features.Rules.Commands.CreateRuleEntry;
using TTRPGHub.Features.Rules.Commands.DeleteRuleEntry;
using TTRPGHub.Features.Rules.Commands.UpdateRuleEntry;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateGameSystemCommandHandlerTests
{
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateGameSystemCommandHandler CreateHandler() => new(_systems, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_SlugAvailable_CreatesCustomSystemOwnedByCurrentUser()
    {
        var creatorId = UserId.New();
        _currentUser.Id.Returns(creatorId);
        _systems.ExistsAsync(Arg.Any<string>()).Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new CreateGameSystemCommand("Blades in the Dark"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _systems.Received(1).AddAsync(
            Arg.Is<GameSystem>(s => !s.IsOfficial && s.CreatedByUserId == creatorId && s.Slug == "blades-in-the-dark"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SlugTaken_AppendsNumericSuffix()
    {
        _currentUser.Id.Returns(UserId.New());
        _systems.ExistsAsync("blades-in-the-dark").Returns(true);
        _systems.ExistsAsync("blades-in-the-dark-2").Returns(false);
        GameSystem? captured = null;
        _systems.When(s => s.AddAsync(Arg.Any<GameSystem>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<GameSystem>());
        var handler = CreateHandler();

        await handler.Handle(new CreateGameSystemCommand("Blades in the Dark"), CancellationToken.None);

        Assert.Equal("blades-in-the-dark-2", captured!.Slug);
    }
}

public class CreateRuleEntryCommandHandlerTests
{
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entries = Substitute.For<IRuleEntryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private CreateRuleEntryCommandHandler CreateHandler() => new(_systems, _entries, _currentUser, _unitOfWork);

    private static CreateRuleEntryCommand ValidCommand(string slug = "pf2e") => new(
        slug, RuleCategory.Class, "Gunslinger", "A gun-toting class", "Full markdown here", "{}", []);

    [Fact]
    public async Task Handle_SystemNotFound_ReturnsNotFound()
    {
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns((GameSystem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OfficialSystem_ReturnsForbidden()
    {
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(GameSystem.CreateOfficial("pf2e", "Pathfinder 2e"));
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _entries.DidNotReceive().AddAsync(Arg.Any<RuleEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CustomSystemOwnedBySomeoneElse_ReturnsForbidden()
    {
        var system = GameSystem.CreateCustom("homebrew-system", "Homebrew System", UserId.New());
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand("homebrew-system"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OwnerOfCustomSystem_CreatesHomebrewEntry()
    {
        var ownerId = UserId.New();
        var system = GameSystem.CreateCustom("homebrew-system", "Homebrew System", ownerId);
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        _currentUser.Id.Returns(ownerId);
        _entries.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns((RuleEntry?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand("homebrew-system"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _entries.Received(1).AddAsync(
            Arg.Is<RuleEntry>(e => e.SystemId == system.Id && e.IsHomebrew && e.Slug == "gunslinger"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SlugTaken_AppendsNumericSuffix()
    {
        var ownerId = UserId.New();
        var system = GameSystem.CreateCustom("homebrew-system", "Homebrew System", ownerId);
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        _currentUser.Id.Returns(ownerId);
        _entries.GetBySlugAsync(system.Id, RuleCategory.Class, "gunslinger").Returns(
            RuleEntry.Create(system.Id, RuleCategory.Class, "gunslinger", "Gunslinger", null, null, "{}", [], true, "Homebrew"));
        _entries.GetBySlugAsync(system.Id, RuleCategory.Class, "gunslinger-2").Returns((RuleEntry?)null);
        RuleEntry? captured = null;
        _entries.When(e => e.AddAsync(Arg.Any<RuleEntry>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<RuleEntry>());
        var handler = CreateHandler();

        await handler.Handle(ValidCommand("homebrew-system"), CancellationToken.None);

        Assert.Equal("gunslinger-2", captured!.Slug);
    }
}

public class UpdateRuleEntryCommandHandlerTests
{
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entries = Substitute.For<IRuleEntryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private UpdateRuleEntryCommandHandler CreateHandler() => new(_systems, _entries, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_OfficialSystem_ReturnsForbidden()
    {
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(GameSystem.CreateOfficial("pf2e", "Pathfinder 2e"));
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateRuleEntryCommand("pf2e", RuleCategory.Class, "fighter", "Fighter", null, null, "{}", []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_EntryNotFound_ReturnsNotFound()
    {
        var ownerId = UserId.New();
        var system = GameSystem.CreateCustom("homebrew-system", "Homebrew", ownerId);
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        _currentUser.Id.Returns(ownerId);
        _entries.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns((RuleEntry?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateRuleEntryCommand("homebrew-system", RuleCategory.Class, "gunslinger", "Gunslinger", null, null, "{}", []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OwnerUpdatingEntry_Succeeds()
    {
        var ownerId = UserId.New();
        var system = GameSystem.CreateCustom("homebrew-system", "Homebrew", ownerId);
        var entry = RuleEntry.Create(system.Id, RuleCategory.Class, "gunslinger", "Gunslinger", null, null, "{}", [], true, "Homebrew");
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        _entries.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns(entry);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateRuleEntryCommand("homebrew-system", RuleCategory.Class, "gunslinger", "Gunslinger v2", "Updated summary", null, "{}", []),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Gunslinger v2", entry.Title);
        _entries.Received(1).Update(entry);
    }
}

public class DeleteRuleEntryCommandHandlerTests
{
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entries = Substitute.For<IRuleEntryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteRuleEntryCommandHandler CreateHandler() => new(_systems, _entries, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_CustomSystemOwnedBySomeoneElse_ReturnsForbidden()
    {
        var system = GameSystem.CreateCustom("homebrew-system", "Homebrew", UserId.New());
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new DeleteRuleEntryCommand("homebrew-system", RuleCategory.Class, "gunslinger"), CancellationToken.None);

        Assert.True(result.IsFailure);
        _entries.DidNotReceive().Remove(Arg.Any<RuleEntry>());
    }

    [Fact]
    public async Task Handle_Owner_RemovesEntry()
    {
        var ownerId = UserId.New();
        var system = GameSystem.CreateCustom("homebrew-system", "Homebrew", ownerId);
        var entry = RuleEntry.Create(system.Id, RuleCategory.Class, "gunslinger", "Gunslinger", null, null, "{}", [], true, "Homebrew");
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        _entries.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns(entry);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new DeleteRuleEntryCommand("homebrew-system", RuleCategory.Class, "gunslinger"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _entries.Received(1).Remove(entry);
    }
}
