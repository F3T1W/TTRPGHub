using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Characters.Queries.CalculateMulticlass;
using TTRPGHub.Features.Characters.Queries.GetCharacterDetail;
using TTRPGHub.Features.GameTable.Queries.GetJournalEntries;
using TTRPGHub.Features.Tickets.Queries.GetTicketById;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class GetCharacterDetailQueryHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetCharacterDetailQueryHandler CreateHandler() => new(_characterRepository, _userRepository, _currentUser);

    [Fact]
    public async Task Handle_CharacterNotFound_ReturnsNotFound()
    {
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns((Character?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCharacterDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_PrivateCharacterViewedByStranger_ReturnsUnauthorized()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCharacterDetailQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_PrivateCharacterViewedByOwner_Succeeds()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCharacterDetailQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Grog", result.Value!.Name);
    }

    [Fact]
    public async Task Handle_PublicCharacterViewedByStranger_Succeeds()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 1).Value!;
        character.SetPublic(true);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCharacterDetailQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_CoOwnerWithMissingUserRecord_UsesPlaceholderUsername()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;
        var coOwnerId = UserId.New();
        character.AddCoOwner(coOwnerId.Value);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _userRepository.GetByIdAsync(Arg.Any<UserId>()).Returns((User?)null);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCharacterDetailQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.CoOwners);
        Assert.Equal("—", result.Value.CoOwners[0].Username);
    }
}

public class GetTicketByIdQueryHandlerTests
{
    private readonly ISupportTicketRepository _tickets = Substitute.For<ISupportTicketRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetTicketByIdQueryHandler CreateHandler() => new(_tickets, _currentUser);

    [Fact]
    public async Task Handle_TicketNotFound_ReturnsNotFound()
    {
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns((SupportTicket?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTicketByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnrelatedPlayer_ReturnsForbidden()
    {
        var ticket = SupportTicket.Create(UserId.New(), "Bug", "Description", null);
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns(ticket);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTicketByIdQuery(ticket.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Reporter_CanViewOwnTicket()
    {
        var reporterId = UserId.New();
        var ticket = SupportTicket.Create(reporterId, "Bug", "Description", null);
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns(ticket);
        _currentUser.Id.Returns(reporterId);
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTicketByIdQuery(ticket.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ModeratorNotInvolved_CanStillView()
    {
        var ticket = SupportTicket.Create(UserId.New(), "Bug", "Description", null);
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns(ticket);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Moderator);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTicketByIdQuery(ticket.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

public class GetJournalEntriesQueryHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly IJournalEntryRepository _journalRepository = Substitute.For<IJournalEntryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetJournalEntriesQueryHandler CreateHandler() => new(_sessionRepository, _journalRepository, _currentUser);

    private static GameSession CreateSession(UserId organizerId) => GameSession.Create(
        organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsNotFound()
    {
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns((GameSession?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetJournalEntriesQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsUnauthorized()
    {
        var session = CreateSession(UserId.New());
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetJournalEntriesQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Organizer_SeesUnpublishedEntries()
    {
        var organizerId = UserId.New();
        var session = CreateSession(organizerId);
        var unpublished = JournalEntry.Create(session.Id, organizerId, "GM secret", "Body");
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _journalRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns((IReadOnlyList<JournalEntry>)[unpublished]);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetJournalEntriesQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task Handle_PlayerCannotSeeUnpublishedEntry()
    {
        var organizerId = UserId.New();
        var playerId = UserId.New();
        var session = CreateSession(organizerId);
        session.Join(playerId);
        var unpublished = JournalEntry.Create(session.Id, organizerId, "GM secret", "Body");
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _journalRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns((IReadOnlyList<JournalEntry>)[unpublished]);
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetJournalEntriesQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Handle_PlayerSeesPublishedEntryRestrictedToSomeoneElse_IsHidden()
    {
        var organizerId = UserId.New();
        var playerId = UserId.New();
        var otherPlayerId = UserId.New();
        var session = CreateSession(organizerId);
        session.Join(playerId);
        session.Join(otherPlayerId);
        var entry = JournalEntry.Create(session.Id, organizerId, "Private note", "Body");
        entry.SetPublished(true);
        entry.SetVisibility($"[\"{otherPlayerId.Value}\"]");
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _journalRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns((IReadOnlyList<JournalEntry>)[entry]);
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetJournalEntriesQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task Handle_PlayerSeesPublishedEntryWithNoVisibilityRestriction()
    {
        var organizerId = UserId.New();
        var playerId = UserId.New();
        var session = CreateSession(organizerId);
        session.Join(playerId);
        var entry = JournalEntry.Create(session.Id, organizerId, "Public note", "Body");
        entry.SetPublished(true);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _journalRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns((IReadOnlyList<JournalEntry>)[entry]);
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetJournalEntriesQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class CalculateMulticlassQueryHandlerTests
{
    private readonly IGameSystemRepository _systemRepository = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entryRepository = Substitute.For<IRuleEntryRepository>();

    private CalculateMulticlassQueryHandler CreateHandler() => new(_systemRepository, _entryRepository);

    [Fact]
    public async Task Handle_NoClasses_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new CalculateMulticlassQuery("dnd5e", []), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SystemNotFound_ReturnsNotFound()
    {
        _systemRepository.GetBySlugAsync(Arg.Any<string>()).Returns((GameSystem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CalculateMulticlassQuery("dnd5e", [new ClassLevelInput("fighter", 3)]), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_LevelBelowOne_ReturnsValidationError()
    {
        var system = GameSystem.CreateOfficial("dnd5e", "D&D 5e");
        _systemRepository.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CalculateMulticlassQuery("dnd5e", [new ClassLevelInput("fighter", 0)]), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnknownClassSlug_ReturnsNotFound()
    {
        var system = GameSystem.CreateOfficial("dnd5e", "D&D 5e");
        _systemRepository.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        _entryRepository.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns((RuleEntry?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CalculateMulticlassQuery("dnd5e", [new ClassLevelInput("nonexistent", 3)]), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_TotalLevelAboveTwenty_ReturnsValidationError()
    {
        var system = GameSystem.CreateOfficial("dnd5e", "D&D 5e");
        _systemRepository.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        var fighter = RuleEntry.Create(system.Id, RuleCategory.Class, "fighter", "Fighter", null, null,
            """{"hit_dice":"1d10"}""", [], false, "SRD");
        _entryRepository.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns(fighter);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CalculateMulticlassQuery("dnd5e", [new ClassLevelInput("fighter", 21)]), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SingleClass_FirstLevelUsesMaxHitDie()
    {
        var system = GameSystem.CreateOfficial("dnd5e", "D&D 5e");
        _systemRepository.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        var fighter = RuleEntry.Create(system.Id, RuleCategory.Class, "fighter", "Fighter", null, null,
            """{"hit_dice":"1d10"}""", [], false, "SRD");
        _entryRepository.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns(fighter);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CalculateMulticlassQuery("dnd5e", [new ClassLevelInput("fighter", 1)]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalLevel);
        Assert.Equal(2, result.Value.ProficiencyBonus);
        Assert.Equal(10, result.Value.Classes[0].AverageHpContribution);
    }

    [Fact]
    public async Task Handle_MultipleClasses_SecondClassUsesAverageForAllItsLevels()
    {
        var system = GameSystem.CreateOfficial("dnd5e", "D&D 5e");
        _systemRepository.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        var fighter = RuleEntry.Create(system.Id, RuleCategory.Class, "fighter", "Fighter", null, null,
            """{"hit_dice":"1d10"}""", [], false, "SRD");
        var wizard = RuleEntry.Create(system.Id, RuleCategory.Class, "wizard", "Wizard", null, null,
            """{"hit_dice":"1d6"}""", [], false, "SRD");
        _entryRepository.GetBySlugAsync(system.Id, RuleCategory.Class, "fighter").Returns(fighter);
        _entryRepository.GetBySlugAsync(system.Id, RuleCategory.Class, "wizard").Returns(wizard);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CalculateMulticlassQuery("dnd5e", [new ClassLevelInput("fighter", 3), new ClassLevelInput("wizard", 2)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.TotalLevel);
        Assert.Equal(3, result.Value.ProficiencyBonus);
        // fighter (first class): level-1 max die (10) + 2 more levels at (10/2+1=6) = 10 + 12 = 22
        Assert.Equal(22, result.Value.Classes[0].AverageHpContribution);
        // wizard (subsequent class): all levels at average (6/2+1=4) * 2 = 8
        Assert.Equal(8, result.Value.Classes[1].AverageHpContribution);
    }

    [Fact]
    public async Task Handle_MissingHitDiceInStats_DefaultsToD8()
    {
        var system = GameSystem.CreateOfficial("dnd5e", "D&D 5e");
        _systemRepository.GetBySlugAsync(Arg.Any<string>()).Returns(system);
        var classEntry = RuleEntry.Create(system.Id, RuleCategory.Class, "custom", "Custom", null, null,
            "{}", [], false, "Homebrew");
        _entryRepository.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns(classEntry);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CalculateMulticlassQuery("dnd5e", [new ClassLevelInput("custom", 1)]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("1d8", result.Value!.Classes[0].HitDice);
        Assert.Equal(8, result.Value.Classes[0].AverageHpContribution);
    }
}
