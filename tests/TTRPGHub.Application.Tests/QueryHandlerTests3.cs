using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Discussions;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Features.Calendar.Queries.GetCalendarFeed;
using TTRPGHub.Features.Calendar.Queries.GetSessionIcs;
using TTRPGHub.Features.Discussions.Queries.GetDiscussion;
using TTRPGHub.Features.Moderation.Queries.GetModerationLog;
using TTRPGHub.Features.Moderation.Queries.GetOpenReports;
using TTRPGHub.Features.Tickets.Queries.GetAllTickets;
using TTRPGHub.Features.Tickets.Queries.GetMyTickets;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class GetMyTicketsQueryHandlerTests
{
    private readonly ISupportTicketRepository _tickets = Substitute.For<ISupportTicketRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetMyTicketsQueryHandler CreateHandler() => new(_tickets, _currentUser);

    [Fact]
    public async Task Handle_ReturnsPagedResultForCurrentUser()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var ticket = SupportTicket.Create(userId, "Bug", "Description", null);
        _tickets.GetByReporterAsync(userId, 1, 20).Returns(((IReadOnlyList<SupportTicket>)[ticket], 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyTicketsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.Total);
    }
}

public class GetAllTicketsQueryHandlerTests
{
    private readonly ISupportTicketRepository _tickets = Substitute.For<ISupportTicketRepository>();

    private GetAllTicketsQueryHandler CreateHandler() => new(_tickets);

    [Fact]
    public async Task Handle_ReturnsAllTickets()
    {
        var ticket = SupportTicket.Create(UserId.New(), "Bug", "Description", null);
        _tickets.GetAllAsync().Returns((IReadOnlyList<SupportTicket>)[ticket]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetAllTicketsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class GetModerationLogQueryHandlerTests
{
    private readonly IModerationLogRepository _log = Substitute.For<IModerationLogRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    private GetModerationLogQueryHandler CreateHandler() => new(_log, _users);

    [Fact]
    public async Task Handle_MapsEntriesWithActorUsername()
    {
        var actorId = UserId.New();
        var entry = ModerationLogEntry.Create(actorId, "DeleteTopic", "ForumTopic", Guid.NewGuid());
        _log.GetRecentAsync(200).Returns((IReadOnlyList<ModerationLogEntry>)[entry]);
        _users.GetByIdAsync(actorId).Returns(TTRPGHub.Entities.User.Create(
            "moderator", TTRPGHub.ValueObjects.Email.Create("mod@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetModerationLogQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("moderator", result.Value![0].ActorUsername);
    }

    [Fact]
    public async Task Handle_MissingActor_UsesPlaceholderUsername()
    {
        var entry = ModerationLogEntry.Create(UserId.New(), "DeleteTopic", "ForumTopic", Guid.NewGuid());
        _log.GetRecentAsync(200).Returns((IReadOnlyList<ModerationLogEntry>)[entry]);
        _users.GetByIdAsync(Arg.Any<UserId>()).Returns((TTRPGHub.Entities.User?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetModerationLogQuery(), CancellationToken.None);

        Assert.Equal("—", result.Value![0].ActorUsername);
    }
}

public class GetOpenReportsQueryHandlerTests
{
    private readonly IContentReportRepository _reports = Substitute.For<IContentReportRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    private GetOpenReportsQueryHandler CreateHandler() => new(_reports, _users);

    [Fact]
    public async Task Handle_MapsOpenReportsWithReporterUsername()
    {
        var reporterId = UserId.New();
        var report = ContentReport.Create(reporterId, ReportedEntityType.ForumPost, Guid.NewGuid(), "Spam");
        _reports.GetOpenAsync().Returns((IReadOnlyList<ContentReport>)[report]);
        _users.GetByIdAsync(reporterId).Returns(TTRPGHub.Entities.User.Create(
            "reporter", TTRPGHub.ValueObjects.Email.Create("reporter@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetOpenReportsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("reporter", result.Value![0].ReporterUsername);
    }
}

public class GetDiscussionQueryHandlerTests
{
    private readonly IDiscussionRepository _repository = Substitute.For<IDiscussionRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetDiscussionQueryHandler CreateHandler() => new(_repository, _currentUser);

    [Fact]
    public async Task Handle_InvalidEntityType_ReturnsValidationError()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new GetDiscussionQuery("NotAType", "some-slug"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NestsRepliesUnderParentPost()
    {
        _currentUser.Id.Returns(UserId.New());
        var author = TTRPGHub.Entities.User.Create("author", TTRPGHub.ValueObjects.Email.Create("author@test.com").Value!, "hash");
        var root = DiscussionPost.Create(DiscussionEntityType.Spell, "fireball", author.Id, "Root comment");
        typeof(DiscussionPost).GetProperty("Author")!.SetValue(root, author);
        var reply = DiscussionPost.Create(DiscussionEntityType.Spell, "fireball", author.Id, "Reply", root.Id);
        typeof(DiscussionPost).GetProperty("Author")!.SetValue(reply, author);
        _repository.GetByEntityAsync(DiscussionEntityType.Spell, "fireball").Returns((IReadOnlyList<DiscussionPost>)[root, reply]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetDiscussionQuery("Spell", "fireball"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Single(result.Value![0].Replies);
    }
}

public class GetSessionIcsQueryHandlerTests
{
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();
    private readonly IUserCalendarPreferenceRepository _preferenceRepository = Substitute.For<IUserCalendarPreferenceRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetSessionIcsQueryHandler CreateHandler() => new(_sessionRepository, _preferenceRepository, _currentUser);

    [Fact]
    public async Task Handle_SessionNotFound_ReturnsNotFound()
    {
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns((GameSession?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSessionIcsQuery(Guid.NewGuid(), 30), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ExplicitReminder_ProducesIcsWithSessionTitle()
    {
        var session = GameSession.Create(UserId.New(), "Curse of the Crimson Throne", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSessionIcsQuery(session.Id.Value, 30), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("BEGIN:VCALENDAR", result.Value);
        Assert.Contains("TRIGGER:-PT30M", result.Value);
    }

    [Fact]
    public async Task Handle_NoReminderProvided_FallsBackToUserPreference()
    {
        var session = GameSession.Create(UserId.New(), "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        var userId = UserId.New();
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.Id.Returns(userId);
        var pref = UserCalendarPreference.Create(userId, 15);
        _preferenceRepository.GetByUserIdAsync(userId).Returns(pref);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSessionIcsQuery(session.Id.Value, 0), CancellationToken.None);

        Assert.Contains("TRIGGER:-PT15M", result.Value);
    }
}

public class GetCalendarFeedQueryHandlerTests
{
    private readonly IUserCalendarPreferenceRepository _preferenceRepository = Substitute.For<IUserCalendarPreferenceRepository>();
    private readonly IGameSessionRepository _sessionRepository = Substitute.For<IGameSessionRepository>();

    private GetCalendarFeedQueryHandler CreateHandler() => new(_preferenceRepository, _sessionRepository);

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNotFound()
    {
        _preferenceRepository.GetByTokenAsync(Arg.Any<Guid>()).Returns((UserCalendarPreference?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCalendarFeedQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidToken_BuildsFeedForUsersSessions()
    {
        var userId = UserId.New();
        var pref = UserCalendarPreference.Create(userId, 60);
        _preferenceRepository.GetByTokenAsync(Arg.Any<Guid>()).Returns(pref);
        var session = GameSession.Create(userId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessionRepository.GetByParticipantAsync(userId).Returns((IReadOnlyList<GameSession>)[session]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCalendarFeedQuery(pref.CalendarToken), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("BEGIN:VCALENDAR", result.Value);
        Assert.Contains("Test", result.Value);
    }
}
