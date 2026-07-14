using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Entities.Ratings;
using TTRPGHub.Features.Homebrew.Commands.DeleteHomebrew;
using TTRPGHub.Features.Homebrew.Commands.ToggleHomebrewLike;
using TTRPGHub.Features.Ratings.Commands.DeleteRating;
using TTRPGHub.Features.Ratings.Commands.DeleteSessionReview;
using TTRPGHub.Features.Ratings.Commands.RateSessionParticipant;
using TTRPGHub.Features.Ratings.Commands.RateUser;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class DeleteHomebrewCommandHandlerTests
{
    private readonly IHomebrewRepository _homebrew = Substitute.For<IHomebrewRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private DeleteHomebrewCommandHandler CreateHandler() => new(_homebrew, _unitOfWork, _currentUser);

    private static HomebrewItem CreateItem(UserId authorId) =>
        HomebrewItem.Create(authorId, "Fireball 2", "A bigger fireball", "pf2e", HomebrewType.Spell, "{}", "fire,evocation");

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsNotFound()
    {
        _homebrew.GetByIdAsync(Arg.Any<HomebrewItemId>()).Returns((HomebrewItem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteHomebrewCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnrelatedPlayer_ReturnsForbidden()
    {
        var item = CreateItem(UserId.New());
        _homebrew.GetByIdAsync(Arg.Any<HomebrewItemId>()).Returns(item);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteHomebrewCommand(item.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        _homebrew.DidNotReceive().Remove(Arg.Any<HomebrewItem>());
    }

    [Fact]
    public async Task Handle_Author_RemovesItem()
    {
        var authorId = UserId.New();
        var item = CreateItem(authorId);
        _homebrew.GetByIdAsync(Arg.Any<HomebrewItemId>()).Returns(item);
        _currentUser.Id.Returns(authorId);
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteHomebrewCommand(item.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _homebrew.Received(1).Remove(item);
    }

    [Fact]
    public async Task Handle_ModeratorDeletingOthersItem_Succeeds()
    {
        var item = CreateItem(UserId.New());
        _homebrew.GetByIdAsync(Arg.Any<HomebrewItemId>()).Returns(item);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Moderator);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteHomebrewCommand(item.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _homebrew.Received(1).Remove(item);
    }
}

public class ToggleHomebrewLikeCommandHandlerTests
{
    private readonly IHomebrewRepository _homebrew = Substitute.For<IHomebrewRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private ToggleHomebrewLikeCommandHandler CreateHandler() => new(_homebrew, _unitOfWork, _currentUser);

    private static HomebrewItem CreateItem() =>
        HomebrewItem.Create(UserId.New(), "Fireball 2", "A bigger fireball", "pf2e", HomebrewType.Spell, "{}", "fire");

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsNotFound()
    {
        _homebrew.GetByIdAsync(Arg.Any<HomebrewItemId>()).Returns((HomebrewItem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new ToggleHomebrewLikeCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NotYetLiked_AddsLikeAndIncrementsCount()
    {
        var item = CreateItem();
        _homebrew.GetByIdAsync(Arg.Any<HomebrewItemId>()).Returns(item);
        _homebrew.HasLikeAsync(Arg.Any<HomebrewItemId>(), Arg.Any<UserId>()).Returns(false);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new ToggleHomebrewLikeCommand(item.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Liked);
        Assert.Equal(1, result.Value.LikeCount);
        _homebrew.Received(1).AddLike(Arg.Any<HomebrewLike>());
    }

}

public class RateUserCommandHandlerTests
{
    private readonly IRatingRepository _ratingRepo = Substitute.For<IRatingRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RateUserCommandHandler CreateHandler() => new(_ratingRepo, _userRepo, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_RatingSelf_ReturnsValidationError()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var handler = CreateHandler();

        var result = await handler.Handle(new RateUserCommand(userId.Value, 5, "Отлично", "Player"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnknownRole_ReturnsValidationError()
    {
        _currentUser.Id.Returns(UserId.New());
        _userRepo.GetByIdAsync(Arg.Any<UserId>()).Returns(User.Create("target", Email.Create("target@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(new RateUserCommand(UserId.New().Value, 5, "Отлично", "Wizard"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NewRating_CreatesRating()
    {
        _currentUser.Id.Returns(UserId.New());
        var ratee = User.Create("target", Email.Create("target@test.com").Value!, "hash");
        _userRepo.GetByIdAsync(Arg.Any<UserId>()).Returns(ratee);
        _ratingRepo.GetByRaterAndRateeAsync(Arg.Any<UserId>(), Arg.Any<UserId>()).Returns((UserRating?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new RateUserCommand(ratee.Id.Value, 5, "Отлично играли", "Player"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _ratingRepo.Received(1).AddAsync(Arg.Any<UserRating>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingRating_UpdatesInsteadOfCreating()
    {
        var raterId = UserId.New();
        _currentUser.Id.Returns(raterId);
        var ratee = User.Create("target", Email.Create("target@test.com").Value!, "hash");
        _userRepo.GetByIdAsync(Arg.Any<UserId>()).Returns(ratee);
        var existing = UserRating.Create(raterId, ratee.Id, 3, "Ok", RatingRole.Player);
        _ratingRepo.GetByRaterAndRateeAsync(Arg.Any<UserId>(), Arg.Any<UserId>()).Returns(existing);
        var handler = CreateHandler();

        var result = await handler.Handle(new RateUserCommand(ratee.Id.Value, 5, "Стало лучше", "Player"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(existing.Id.Value, result.Value);
        Assert.Equal(5, existing.Score);
        await _ratingRepo.DidNotReceive().AddAsync(Arg.Any<UserRating>(), Arg.Any<CancellationToken>());
    }
}

public class DeleteRatingCommandHandlerTests
{
    private readonly IRatingRepository _ratingRepo = Substitute.For<IRatingRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteRatingCommandHandler CreateHandler() => new(_ratingRepo, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_UnrelatedPlayer_ReturnsForbidden()
    {
        var rating = UserRating.Create(UserId.New(), UserId.New(), 4, "Ok", RatingRole.Player);
        _ratingRepo.GetByIdAsync(Arg.Any<UserRatingId>()).Returns(rating);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteRatingCommand(rating.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Rater_RemovesOwnRating()
    {
        var raterId = UserId.New();
        var rating = UserRating.Create(raterId, UserId.New(), 4, "Ok", RatingRole.Player);
        _ratingRepo.GetByIdAsync(Arg.Any<UserRatingId>()).Returns(rating);
        _currentUser.Id.Returns(raterId);
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteRatingCommand(rating.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _ratingRepo.Received(1).Remove(rating);
    }

    [Fact]
    public async Task Handle_ModeratorRemovingOthersRating_Succeeds()
    {
        var rating = UserRating.Create(UserId.New(), UserId.New(), 4, "Ok", RatingRole.Player);
        _ratingRepo.GetByIdAsync(Arg.Any<UserRatingId>()).Returns(rating);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Moderator);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteRatingCommand(rating.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _ratingRepo.Received(1).Remove(rating);
    }
}

public class RateSessionParticipantCommandHandlerTests
{
    private readonly IGameSessionRepository _sessions = Substitute.For<IGameSessionRepository>();
    private readonly ISessionReviewRepository _reviews = Substitute.For<ISessionReviewRepository>();
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private RateSessionParticipantCommandHandler CreateHandler() =>
        new(_sessions, _reviews, _userRepo, _currentUser, _unitOfWork);

    private static GameSession CreateCompletedSession(UserId organizerId, UserId reviewerId, UserId revieweeId)
    {
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(-1), SessionFormat.Online, null);
        if (reviewerId != organizerId) session.Join(reviewerId);
        if (revieweeId != organizerId) session.Join(revieweeId);
        session.Start(organizerId);
        session.Complete(organizerId);
        return session;
    }

    [Fact]
    public async Task Handle_RatingSelf_ReturnsValidationError()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RateSessionParticipantCommand(Guid.NewGuid(), userId.Value, 5, "Nice"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_SessionNotCompleted_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _sessions.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RateSessionParticipantCommand(session.Id.Value, UserId.New().Value, 5, "Nice"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ReviewerNotParticipant_ReturnsForbidden()
    {
        var organizerId = UserId.New();
        var revieweeId = UserId.New();
        var session = CreateCompletedSession(organizerId, organizerId, revieweeId);
        _sessions.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RateSessionParticipantCommand(session.Id.Value, revieweeId.Value, 5, "Nice"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_RevieweeNotParticipant_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var session = CreateCompletedSession(organizerId, organizerId, organizerId);
        _sessions.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RateSessionParticipantCommand(session.Id.Value, UserId.New().Value, 5, "Nice"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ValidReview_CreatesReview()
    {
        var organizerId = UserId.New();
        var revieweeId = UserId.New();
        var session = CreateCompletedSession(organizerId, organizerId, revieweeId);
        _sessions.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _currentUser.Id.Returns(organizerId);
        _userRepo.GetByIdAsync(Arg.Any<UserId>()).Returns(User.Create("reviewee", Email.Create("reviewee@test.com").Value!, "hash"));
        _reviews.GetBySessionReviewerRevieweeAsync(Arg.Any<GameSessionId>(), Arg.Any<UserId>(), Arg.Any<UserId>())
            .Returns((SessionReview?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RateSessionParticipantCommand(session.Id.Value, revieweeId.Value, 5, "Great DM"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _reviews.Received(1).AddAsync(Arg.Any<SessionReview>(), Arg.Any<CancellationToken>());
    }
}

public class DeleteSessionReviewCommandHandlerTests
{
    private readonly ISessionReviewRepository _reviews = Substitute.For<ISessionReviewRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteSessionReviewCommandHandler CreateHandler() => new(_reviews, _currentUser, _unitOfWork);

    [Fact]
    public async Task Handle_UnrelatedPlayer_ReturnsForbidden()
    {
        var review = SessionReview.Create(GameSessionId.New(), UserId.New(), UserId.New(), 4, "Ok");
        _reviews.GetByIdAsync(Arg.Any<SessionReviewId>()).Returns(review);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteSessionReviewCommand(review.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Reviewer_RemovesOwnReview()
    {
        var reviewerId = UserId.New();
        var review = SessionReview.Create(GameSessionId.New(), reviewerId, UserId.New(), 4, "Ok");
        _reviews.GetByIdAsync(Arg.Any<SessionReviewId>()).Returns(review);
        _currentUser.Id.Returns(reviewerId);
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteSessionReviewCommand(review.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _reviews.Received(1).Remove(review);
    }

    [Fact]
    public async Task Handle_ModeratorRemovingOthersReview_Succeeds()
    {
        var review = SessionReview.Create(GameSessionId.New(), UserId.New(), UserId.New(), 4, "Ok");
        _reviews.GetByIdAsync(Arg.Any<SessionReviewId>()).Returns(review);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Moderator);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteSessionReviewCommand(review.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _reviews.Received(1).Remove(review);
    }
}
