using System.Reflection;
using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Features.Homebrew.Queries.GetHomebrewDetail;
using TTRPGHub.Features.Homebrew.Queries.SearchHomebrew;
using TTRPGHub.Features.Ratings.Queries.GetSessionReviews;
using TTRPGHub.Features.Ratings.Queries.GetUserRatings;
using TTRPGHub.Features.Ratings.Queries.GetUserSessionReviews;
using TTRPGHub.Features.SessionNotes.Queries.GetNoteDetail;
using TTRPGHub.Features.SessionNotes.Queries.GetNotesByCampaign;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

internal static class NavigationPropertySetter
{
    internal static void SetAuthor(HomebrewItem item, User author) =>
        typeof(HomebrewItem).GetProperty("Author")!.SetValue(item, author);

    internal static void SetRater(UserRating rating, User rater) =>
        typeof(UserRating).GetProperty("Rater")!.SetValue(rating, rater);

    internal static void SetReviewer(SessionReview review, User reviewer) =>
        typeof(SessionReview).GetProperty("Reviewer")!.SetValue(review, reviewer);
}

public class GetHomebrewDetailQueryHandlerTests
{
    private readonly IHomebrewRepository _homebrew = Substitute.For<IHomebrewRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetHomebrewDetailQueryHandler CreateHandler() => new(_homebrew, _currentUser);

    private static HomebrewItem CreateItem()
    {
        var item = HomebrewItem.Create(UserId.New(), "Fireball 2", "Bigger fireball", "pf2e", HomebrewType.Spell, "{}", "fire");
        NavigationPropertySetter.SetAuthor(item, User.Create("author", Email.Create("author@test.com").Value!, "hash"));
        return item;
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsNotFound()
    {
        _homebrew.GetByIdAsync(Arg.Any<HomebrewItemId>()).Returns((HomebrewItem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetHomebrewDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_ReturnsDetailWithLikedByMeFalse()
    {
        var item = CreateItem();
        _homebrew.GetByIdAsync(Arg.Any<HomebrewItemId>()).Returns(item);
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetHomebrewDetailQuery(item.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.LikedByMe);
        Assert.Equal("author", result.Value.AuthorUsername);
    }
}

public class SearchHomebrewQueryHandlerTests
{
    private readonly IHomebrewRepository _homebrew = Substitute.For<IHomebrewRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private SearchHomebrewQueryHandler CreateHandler() => new(_homebrew, _currentUser);

    [Fact]
    public async Task Handle_ReturnsPagedResults()
    {
        var item = HomebrewItem.Create(UserId.New(), "Fireball 2", "Bigger fireball", "pf2e", HomebrewType.Spell, "{}", "fire");
        NavigationPropertySetter.SetAuthor(item, User.Create("author", Email.Create("author@test.com").Value!, "hash"));
        _homebrew.SearchAsync(null, null, null, null, 1, 20).Returns((new List<HomebrewItem> { item }, 1));
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new SearchHomebrewQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.Total);
    }
}

public class GetNoteDetailQueryHandlerTests
{
    private readonly ISessionNoteRepository _noteRepository = Substitute.For<ISessionNoteRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetNoteDetailQueryHandler CreateHandler() => new(_noteRepository, _userRepository, _currentUser);

    [Fact]
    public async Task Handle_NoteNotFound_ReturnsNotFound()
    {
        _noteRepository.GetByIdAsync(Arg.Any<SessionNoteId>()).Returns((SessionNote?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetNoteDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MarksIsAuthorWhenCurrentUserWroteIt()
    {
        var authorId = UserId.New();
        var note = SessionNote.Create(CampaignId.New(), authorId, "Session 3", "Recap", DateTime.UtcNow);
        _noteRepository.GetByIdAsync(Arg.Any<SessionNoteId>()).Returns(note);
        _userRepository.GetByIdAsync(authorId).Returns(User.Create("gm", Email.Create("gm@test.com").Value!, "hash"));
        _currentUser.Id.Returns(authorId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetNoteDetailQuery(note.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsAuthor);
        Assert.Equal("gm", result.Value.AuthorName);
    }
}

public class GetNotesByCampaignQueryHandlerTests
{
    private readonly ISessionNoteRepository _noteRepository = Substitute.For<ISessionNoteRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private GetNotesByCampaignQueryHandler CreateHandler() => new(_noteRepository, _userRepository);

    [Fact]
    public async Task Handle_OrdersBySessionDateDescending()
    {
        var campaignId = CampaignId.New();
        var authorId = UserId.New();
        var older = SessionNote.Create(campaignId, authorId, "Session 1", "Recap 1", DateTime.UtcNow.AddDays(-14));
        var newer = SessionNote.Create(campaignId, authorId, "Session 2", "Recap 2", DateTime.UtcNow.AddDays(-1));
        _noteRepository.GetByCampaignAsync(campaignId).Returns((IReadOnlyList<SessionNote>)[older, newer]);
        _userRepository.GetByIdAsync(authorId).Returns(User.Create("gm", Email.Create("gm@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetNotesByCampaignQuery(campaignId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Session 2", result.Value![0].Title);
        Assert.Equal("Session 1", result.Value[1].Title);
    }
}

public class GetUserRatingsQueryHandlerTests
{
    private readonly IRatingRepository _ratings = Substitute.For<IRatingRepository>();

    private GetUserRatingsQueryHandler CreateHandler() => new(_ratings);

    [Fact]
    public async Task Handle_ReturnsRatingsWithStats()
    {
        var rateeId = UserId.New();
        var rating = UserRating.Create(UserId.New(), rateeId, 5, "Great DM", RatingRole.DungeonMaster);
        NavigationPropertySetter.SetRater(rating, User.Create("rater", Email.Create("rater@test.com").Value!, "hash"));
        _ratings.GetByRateeAsync(rateeId).Returns([rating]);
        _ratings.GetStatsAsync(rateeId).Returns((5.0, 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserRatingsQuery(rateeId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Ratings);
        Assert.Equal(5.0, result.Value.AverageScore);
        Assert.Equal("rater", result.Value.Ratings[0].RaterUsername);
    }
}

public class GetUserSessionReviewsQueryHandlerTests
{
    private readonly ISessionReviewRepository _reviews = Substitute.For<ISessionReviewRepository>();
    private readonly IGameSessionRepository _sessions = Substitute.For<IGameSessionRepository>();

    private GetUserSessionReviewsQueryHandler CreateHandler() => new(_reviews, _sessions);

    [Fact]
    public async Task Handle_ReturnsReviewsWithSessionTitle()
    {
        var revieweeId = UserId.New();
        var session = GameSession.Create(UserId.New(), "Curse of the Crimson Throne", null, "pf2e", 4, DateTime.UtcNow.AddDays(-1), SessionFormat.Online, null);
        var review = SessionReview.Create(session.Id, UserId.New(), revieweeId, 5, "Great session");
        NavigationPropertySetter.SetReviewer(review, User.Create("reviewer", Email.Create("reviewer@test.com").Value!, "hash"));
        _reviews.GetByRevieweeAsync(revieweeId).Returns([review]);
        _reviews.GetStatsAsync(revieweeId).Returns((5.0, 1));
        _sessions.GetByIdAsync(session.Id).Returns(session);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserSessionReviewsQuery(revieweeId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Curse of the Crimson Throne", result.Value!.Reviews[0].SessionTitle);
    }

    [Fact]
    public async Task Handle_SessionDeleted_UsesPlaceholderTitle()
    {
        var revieweeId = UserId.New();
        var review = SessionReview.Create(GameSessionId.New(), UserId.New(), revieweeId, 4, "Ok");
        _reviews.GetByRevieweeAsync(revieweeId).Returns([review]);
        _reviews.GetStatsAsync(revieweeId).Returns((4.0, 1));
        _sessions.GetByIdAsync(Arg.Any<GameSessionId>()).Returns((GameSession?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserSessionReviewsQuery(revieweeId.Value), CancellationToken.None);

        Assert.Equal("—", result.Value!.Reviews[0].SessionTitle);
    }
}

public class GetSessionReviewsQueryHandlerTests
{
    private readonly ISessionReviewRepository _reviews = Substitute.For<ISessionReviewRepository>();
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    private GetSessionReviewsQueryHandler CreateHandler() => new(_reviews, _users);

    [Fact]
    public async Task Handle_ReturnsReviewsWithRevieweeUsername()
    {
        var sessionId = GameSessionId.New();
        var revieweeId = UserId.New();
        var review = SessionReview.Create(sessionId, UserId.New(), revieweeId, 5, "Great session");
        NavigationPropertySetter.SetReviewer(review, User.Create("reviewer", Email.Create("reviewer@test.com").Value!, "hash"));
        _reviews.GetBySessionAsync(sessionId).Returns([review]);
        _users.GetByIdAsync(revieweeId).Returns(User.Create("reviewee", Email.Create("reviewee@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSessionReviewsQuery(sessionId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("reviewee", result.Value![0].RevieweeUsername);
        Assert.Equal("reviewer", result.Value[0].ReviewerUsername);
    }
}
