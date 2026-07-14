using System.Text.Json;
using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Modules.Commands.ExportModule;
using TTRPGHub.Features.Modules.Commands.ImportModule;
using TTRPGHub.Features.Modules.Shared;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class ExportModuleCommandHandlerTests
{
    private readonly IMacroRepository _macros = Substitute.For<IMacroRepository>();
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entries = Substitute.For<IRuleEntryRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private ExportModuleCommandHandler CreateHandler() => new(_macros, _systems, _entries, _users, _currentUser);

    [Fact]
    public async Task Handle_NoMacrosAndNoSystem_ReturnsValidationError()
    {
        _currentUser.Id.Returns(UserId.New());
        _macros.GetByOwnerAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<Macro>)[]);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ExportModuleCommand("My Module", null, null, [], null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SystemNotFound_ReturnsNotFound()
    {
        _currentUser.Id.Returns(UserId.New());
        _macros.GetByOwnerAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<Macro>)[]);
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns((GameSystem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ExportModuleCommand("My Module", null, null, [], "missing-system"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OfficialSystem_ReturnsForbidden()
    {
        _currentUser.Id.Returns(UserId.New());
        _macros.GetByOwnerAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<Macro>)[]);
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(GameSystem.CreateOfficial("pf2e", "Pathfinder 2e"));
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ExportModuleCommand("My Module", null, null, [], "pf2e"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SomeoneElsesCustomSystem_ReturnsForbidden()
    {
        _currentUser.Id.Returns(UserId.New());
        _macros.GetByOwnerAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<Macro>)[]);
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns(GameSystem.CreateCustom("homebrew-system", "Homebrew", UserId.New()));
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ExportModuleCommand("My Module", null, null, [], "homebrew-system"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SelectedMacros_ExportsOnlyRequestedOnes()
    {
        var ownerId = UserId.New();
        _currentUser.Id.Returns(ownerId);
        var wanted = Macro.Create(ownerId, "Fireball", null, MacroType.Chat, "/roll 8d6");
        var unwanted = Macro.Create(ownerId, "Unrelated", null, MacroType.Chat, "/roll 1d4");
        _macros.GetByOwnerAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<Macro>)[wanted, unwanted]);
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns(User.Create("gm", Email.Create("gm@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ExportModuleCommand("My Module", "Some spells", "2.0.0", [wanted.Id], null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var manifest = JsonSerializer.Deserialize<ModuleManifest>(result.Value!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(manifest);
        Assert.Single(manifest!.Macros);
        Assert.Equal("Fireball", manifest.Macros[0].Name);
        Assert.Equal("2.0.0", manifest.Version);
        Assert.Equal("gm", manifest.Author);
    }

    [Fact]
    public async Task Handle_NoVersionProvided_DefaultsToOneDotZero()
    {
        var ownerId = UserId.New();
        _currentUser.Id.Returns(ownerId);
        var macro = Macro.Create(ownerId, "Fireball", null, MacroType.Chat, "/roll 8d6");
        _macros.GetByOwnerAsync(Arg.Any<UserId>()).Returns((IReadOnlyList<Macro>)[macro]);
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns(User.Create("gm", Email.Create("gm@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ExportModuleCommand("My Module", null, null, [macro.Id], null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var manifest = JsonSerializer.Deserialize<ModuleManifest>(result.Value!, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.Equal("1.0.0", manifest!.Version);
    }
}

public class ImportModuleCommandHandlerTests
{
    private readonly IMacroRepository _macros = Substitute.For<IMacroRepository>();
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entries = Substitute.For<IRuleEntryRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private ImportModuleCommandHandler CreateHandler() => new(_macros, _systems, _entries, _unitOfWork, _currentUser);

    [Fact]
    public async Task Handle_InvalidJson_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new ImportModuleCommand("not json"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MissingName_ReturnsValidationError()
    {
        var handler = CreateHandler();
        var manifest = new ModuleManifest(1, "", null, null, null, [], null);

        var result = await handler.Handle(new ImportModuleCommand(JsonSerializer.Serialize(manifest)), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_EmptyModule_ReturnsValidationError()
    {
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();
        var manifest = new ModuleManifest(1, "Empty Module", null, null, null, [], null);

        var result = await handler.Handle(new ImportModuleCommand(JsonSerializer.Serialize(manifest)), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidMacros_ImportsThemUnderCurrentUser()
    {
        var currentUserId = UserId.New();
        _currentUser.Id.Returns(currentUserId);
        var handler = CreateHandler();
        var manifest = new ModuleManifest(1, "Combat Macros", null, "SomeGm", null,
            [new ModuleMacro("Fireball", null, "chat", "/roll 8d6"), new ModuleMacro("  ", null, "chat", "/roll 1d4")], null);

        var result = await handler.Handle(new ImportModuleCommand(JsonSerializer.Serialize(manifest)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.MacrosImported);
        await _macros.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<Macro>>(list => list.Count() == 1 && list.First().OwnerId == currentUserId && list.First().Name == "Fireball"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TooManyMacros_ReturnsValidationError()
    {
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();
        var manifest = new ModuleManifest(1, "Huge Module", null, null, null,
            Enumerable.Range(0, 201).Select(i => new ModuleMacro($"Macro {i}", null, "chat", "/roll 1d4")).ToList(), null);

        var result = await handler.Handle(new ImportModuleCommand(JsonSerializer.Serialize(manifest)), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_RuleSystem_CreatesCustomSystemAndEntries()
    {
        var currentUserId = UserId.New();
        _currentUser.Id.Returns(currentUserId);
        _systems.ExistsAsync(Arg.Any<string>()).Returns(false);
        var handler = CreateHandler();
        var ruleSystem = new ModuleRuleSystem("Homebrew Setting", [
            new ModuleRuleEntry("Class", "Gunslinger", "Gun class", null, "{}", []),
        ]);
        var manifest = new ModuleManifest(1, "Setting Pack", null, "SomeGm", null, [], ruleSystem);

        var result = await handler.Handle(new ImportModuleCommand(JsonSerializer.Serialize(manifest)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.RuleEntriesImported);
        Assert.Equal("homebrew-setting", result.Value.SystemSlug);
        await _systems.Received(1).AddAsync(
            Arg.Is<GameSystem>(s => s.CreatedByUserId == currentUserId && !s.IsOfficial), Arg.Any<CancellationToken>());
        await _entries.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<RuleEntry>>(list => list.Count() == 1 && list.First().Title == "Gunslinger"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RuleSystem_SkipsEntriesWithUnknownCategory()
    {
        _currentUser.Id.Returns(UserId.New());
        _systems.ExistsAsync(Arg.Any<string>()).Returns(false);
        var handler = CreateHandler();
        var ruleSystem = new ModuleRuleSystem("Homebrew Setting", [
            new ModuleRuleEntry("NotARealCategory", "Something", null, null, "{}", []),
        ]);
        var manifest = new ModuleManifest(1, "Setting Pack", null, null, null, [], ruleSystem);

        var result = await handler.Handle(new ImportModuleCommand(JsonSerializer.Serialize(manifest)), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SlugCollision_AppendsNumericSuffix()
    {
        _currentUser.Id.Returns(UserId.New());
        _systems.ExistsAsync("homebrew-setting").Returns(true);
        _systems.ExistsAsync("homebrew-setting-2").Returns(false);
        GameSystem? captured = null;
        _systems.When(s => s.AddAsync(Arg.Any<GameSystem>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<GameSystem>());
        var handler = CreateHandler();
        var ruleSystem = new ModuleRuleSystem("Homebrew Setting", [
            new ModuleRuleEntry("Class", "Gunslinger", null, null, "{}", []),
        ]);
        var manifest = new ModuleManifest(1, "Setting Pack", null, null, null, [], ruleSystem);

        await handler.Handle(new ImportModuleCommand(JsonSerializer.Serialize(manifest)), CancellationToken.None);

        Assert.Equal("homebrew-setting-2", captured!.Slug);
    }
}
