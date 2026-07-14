using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Features.Calendar.Queries.GetCalendarPreference;
using TTRPGHub.Features.Characters.Queries.GetCharacterById;
using TTRPGHub.Features.Characters.Queries.GetChronicles;
using TTRPGHub.Features.Characters.Queries.GetCompanionById;
using TTRPGHub.Features.Characters.Queries.GetCompanions;
using TTRPGHub.Features.GameTable.Queries.GetSessionCharacters;
using TTRPGHub.Features.GameTable.Queries.GetTableState;
using TTRPGHub.Features.Tickets.Queries.GetTicketComments;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class GetTicketCommentsQueryHandlerTests
{
    private readonly ISupportTicketRepository _tickets = Substitute.For<ISupportTicketRepository>();
    private readonly ITicketCommentRepository _comments = Substitute.For<ITicketCommentRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetTicketCommentsQueryHandler CreateHandler() => new(_tickets, _comments, _users, _currentUser);

    [Fact]
    public async Task Handle_TicketNotFound_ReturnsNotFound()
    {
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns((SupportTicket?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTicketCommentsQuery(Guid.NewGuid()), CancellationToken.None);

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

        var result = await handler.Handle(new GetTicketCommentsQuery(ticket.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MissingAuthorRecord_UsesPlaceholderUsername()
    {
        var reporterId = UserId.New();
        var ticket = SupportTicket.Create(reporterId, "Bug", "Description", null);
        _tickets.GetByIdAsync(Arg.Any<SupportTicketId>()).Returns(ticket);
        _currentUser.Id.Returns(reporterId);
        _currentUser.Role.Returns(UserRole.Player);
        var comment = TicketComment.Create(ticket.Id, UserId.New(), "Looking into it");
        _comments.GetByTicketAsync(Arg.Any<SupportTicketId>()).Returns([comment]);
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns((User?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTicketCommentsQuery(ticket.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("—", result.Value![0].AuthorUsername);
    }
}

public class GetSessionCharactersQueryHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetSessionCharactersQueryHandler CreateHandler() =>
        new(_sessionRepository, _characterRepository, _userRepository, _currentUser);

    [Fact]
    public async Task Handle_NonOrganizer_ReturnsUnauthorized()
    {
        var session = GameSession.Create(UserId.New(), "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSessionCharactersQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Organizer_CollectsCharactersFromAllParticipants()
    {
        var organizerId = UserId.New();
        var playerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        session.Join(playerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        var playerCharacter = Character.Create(playerId, "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByOwnerAsync(playerId).Returns([playerCharacter]);
        _characterRepository.GetByOwnerAsync(organizerId).Returns((IReadOnlyList<Character>)[]);
        _userRepository.GetByIdAsync(Arg.Any<UserId>()).Returns(User.Create("player", Email.Create("player@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSessionCharactersQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal("Grog", result.Value![0].Name);
    }
}

public class GetCharacterByIdQueryHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetCharacterByIdQueryHandler CreateHandler() => new(_characterRepository, _currentUser);

    [Fact]
    public async Task Handle_PrivateCharacterViewedByStranger_ReturnsUnauthorized()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCharacterByIdQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_PublicCharacter_VisibleToAnyone()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 1).Value!;
        character.SetPublic(true);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCharacterByIdQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

public class GetChroniclesQueryHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly IPathfinderSocietyChronicleRepository _chronicleRepository = Substitute.For<IPathfinderSocietyChronicleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetChroniclesQueryHandler CreateHandler() => new(_characterRepository, _chronicleRepository, _currentUser);

    [Fact]
    public async Task Handle_PrivateCharacterViewedByStranger_ReturnsUnauthorized()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetChroniclesQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsChronicles()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var chronicle = PathfinderSocietyChronicle.Create(
            character.Id, "Scenario 1-01", DateOnly.FromDateTime(DateTime.Today), null, null, 0, 0, null, null).Value!;
        _chronicleRepository.GetByCharacterAsync(Arg.Any<CharacterId>()).Returns([chronicle]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetChroniclesQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class GetCompanionsQueryHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICompanionRepository _companionRepository = Substitute.For<ICompanionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetCompanionsQueryHandler CreateHandler() => new(_characterRepository, _companionRepository, _currentUser);

    [Fact]
    public async Task Handle_PrivateCharacterViewedByStranger_ReturnsUnauthorized()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCompanionsQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsCompanions()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var companion = Companion.Create(character.Id, "Wolf", "Animal Companion", 1, 10, null, null, null, null, null).Value!;
        _companionRepository.GetByCharacterAsync(Arg.Any<CharacterId>()).Returns([companion]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCompanionsQuery(character.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class GetCompanionByIdQueryHandlerTests
{
    private readonly ICompanionRepository _companionRepository = Substitute.For<ICompanionRepository>();
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetCompanionByIdQueryHandler CreateHandler() => new(_companionRepository, _characterRepository, _currentUser);

    [Fact]
    public async Task Handle_ParentCharacterPrivateAndViewedByStranger_ReturnsUnauthorized()
    {
        var character = Character.Create(UserId.New(), "Grog", "Human", "Fighter", 1).Value!;
        var companion = Companion.Create(character.Id, "Wolf", "Animal Companion", 1, 10, null, null, null, null, null).Value!;
        _companionRepository.GetByIdAsync(Arg.Any<CompanionId>()).Returns(companion);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCompanionByIdQuery(companion.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ParentCharacterMissing_ReturnsNotFound()
    {
        var companion = Companion.Create(CharacterId.New(), "Wolf", "Animal Companion", 1, 10, null, null, null, null, null).Value!;
        _companionRepository.GetByIdAsync(Arg.Any<CompanionId>()).Returns(companion);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns((Character?)null);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCompanionByIdQuery(companion.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Owner_ReturnsCompanion()
    {
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 1).Value!;
        var companion = Companion.Create(character.Id, "Wolf", "Animal Companion", 1, 10, null, null, null, null, null).Value!;
        _companionRepository.GetByIdAsync(Arg.Any<CompanionId>()).Returns(companion);
        _characterRepository.GetByIdAsync(Arg.Any<CharacterId>()).Returns(character);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCompanionByIdQuery(companion.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Wolf", result.Value!.Name);
    }
}

public class GetCalendarPreferenceQueryHandlerTests
{
    private readonly IUserCalendarPreferenceRepository _repository = Substitute.For<IUserCalendarPreferenceRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetCalendarPreferenceQueryHandler CreateHandler() => new(_repository, _currentUser);

    [Fact]
    public async Task Handle_Unauthenticated_ReturnsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCalendarPreferenceQuery(), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NoPreferenceYet_ReturnsSensibleDefaults()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(UserId.New());
        _repository.GetByUserIdAsync(Arg.Any<UserId>()).Returns((UserCalendarPreference?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCalendarPreferenceQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(60, result.Value!.ReminderMinutes);
        Assert.False(result.Value.PushEnabled);
        Assert.Equal(Guid.Empty, result.Value.CalendarToken);
    }
}

public class GetTableStateQueryHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly ISceneRepository _sceneRepository = Substitute.For<ISceneRepository>();
    private readonly ITableMessageRepository _messageRepository = Substitute.For<ITableMessageRepository>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetTableStateQueryHandler CreateHandler() =>
        new(_sessionRepository, _sceneRepository, _messageRepository, _tokenRepository, _userRepository, _currentUser);

    private static GameSession CreateInProgressSession(UserId organizerId, params UserId[] players)
    {
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        foreach (var player in players)
            session.Join(player);
        session.Start(organizerId);
        return session;
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsUnauthorized()
    {
        var session = CreateInProgressSession(UserId.New());
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTableStateQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SessionNotInProgress_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTableStateQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NoScenes_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = CreateInProgressSession(organizerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _sceneRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns((IReadOnlyList<Scene>)[]);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTableStateQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_WhisperNotAddressedToCurrentUser_IsExcluded()
    {
        var organizerId = UserId.New();
        var playerId = UserId.New();
        var otherPlayerId = UserId.New();
        var session = CreateInProgressSession(organizerId, playerId, otherPlayerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        var scene = Scene.Create(session.Id, "Scene 1", 0);
        _sceneRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns([scene]);
        var whisper = TableMessage.CreateWhisper(session.Id, organizerId, "GM", otherPlayerId, "Other", "Secret plan");
        var chat = TableMessage.CreateChat(session.Id, organizerId, "GM", "Hello everyone");
        _messageRepository.GetRecentAsync(Arg.Any<GameSessionId>(), Arg.Any<int>()).Returns([whisper, chat]);
        _tokenRepository.GetBySceneAsync(Arg.Any<Guid>()).Returns((IReadOnlyList<TableToken>)[]);
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTableStateQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.RecentMessages);
        Assert.Equal("Hello everyone", result.Value.RecentMessages[0].Content);
    }

    [Fact]
    public async Task Handle_WhisperAddressedToCurrentUser_IsIncluded()
    {
        var organizerId = UserId.New();
        var playerId = UserId.New();
        var session = CreateInProgressSession(organizerId, playerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        var scene = Scene.Create(session.Id, "Scene 1", 0);
        _sceneRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns([scene]);
        var whisper = TableMessage.CreateWhisper(session.Id, organizerId, "GM", playerId, "Player", "Secret plan for you");
        _messageRepository.GetRecentAsync(Arg.Any<GameSessionId>(), Arg.Any<int>()).Returns([whisper]);
        _tokenRepository.GetBySceneAsync(Arg.Any<Guid>()).Returns((IReadOnlyList<TableToken>)[]);
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTableStateQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.ToString());
        Assert.Single(result.Value!.RecentMessages);
    }

    [Fact]
    public async Task Handle_HiddenTokenNotOwnedByPlayer_IsExcluded()
    {
        var organizerId = UserId.New();
        var playerId = UserId.New();
        var session = CreateInProgressSession(organizerId, playerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        var scene = Scene.Create(session.Id, "Scene 1", 0);
        _sceneRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns([scene]);
        _messageRepository.GetRecentAsync(Arg.Any<GameSessionId>(), Arg.Any<int>()).Returns((IReadOnlyList<TableMessage>)[]);
        var hiddenToken = TableToken.Create(session.Id, scene.Id, "Goblin", null, "#f00", 0, 0, UserId.New());
        hiddenToken.SetVisibility("[]");
        _tokenRepository.GetBySceneAsync(Arg.Any<Guid>()).Returns([hiddenToken]);
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTableStateQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Tokens);
    }

    [Fact]
    public async Task Handle_Organizer_SeesAllTokensRegardlessOfVisibility()
    {
        var organizerId = UserId.New();
        var session = CreateInProgressSession(organizerId);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        var scene = Scene.Create(session.Id, "Scene 1", 0);
        _sceneRepository.GetBySessionAsync(Arg.Any<GameSessionId>()).Returns([scene]);
        _messageRepository.GetRecentAsync(Arg.Any<GameSessionId>(), Arg.Any<int>()).Returns((IReadOnlyList<TableMessage>)[]);
        var hiddenToken = TableToken.Create(session.Id, scene.Id, "Goblin", null, "#f00", 0, 0, UserId.New());
        hiddenToken.SetVisibility("[]");
        _tokenRepository.GetBySceneAsync(Arg.Any<Guid>()).Returns([hiddenToken]);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTableStateQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Tokens);
    }
}
