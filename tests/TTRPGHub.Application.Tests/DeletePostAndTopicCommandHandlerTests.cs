using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Discussions;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Forum;
using ForumDeletePostCommand = TTRPGHub.Features.Forum.Commands.DeletePost.DeletePostCommand;
using ForumDeletePostCommandHandler = TTRPGHub.Features.Forum.Commands.DeletePost.DeletePostCommandHandler;
using DiscussionDeletePostCommand = TTRPGHub.Features.Discussions.Commands.DeletePost.DeletePostCommand;
using DiscussionDeletePostCommandHandler = TTRPGHub.Features.Discussions.Commands.DeletePost.DeletePostCommandHandler;
using TTRPGHub.Features.Forum.Commands.DeleteTopic;

namespace TTRPGHub.Application.Tests;

public class ForumDeletePostCommandHandlerTests
{
    private readonly IForumPostRepository _posts = Substitute.For<IForumPostRepository>();
    private readonly IModerationLogRepository _moderationLog = Substitute.For<IModerationLogRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private ForumDeletePostCommandHandler CreateHandler() => new(_posts, _moderationLog, _currentUser, _unitOfWork);

    private static ForumPost CreatePost(UserId authorId) => ForumPost.Create(ForumTopicId.New(), authorId, "Text");

    [Fact]
    public async Task Handle_PostNotFound_ReturnsNotFound()
    {
        _posts.GetByIdAsync(Arg.Any<ForumPostId>()).Returns((ForumPost?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new ForumDeletePostCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnrelatedPlayer_ReturnsForbidden()
    {
        var post = CreatePost(UserId.New());
        _posts.GetByIdAsync(Arg.Any<ForumPostId>()).Returns(post);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new ForumDeletePostCommand(post.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        _posts.DidNotReceive().Remove(Arg.Any<ForumPost>());
    }

    [Fact]
    public async Task Handle_Author_RemovesWithoutLoggingModeration()
    {
        var authorId = UserId.New();
        var post = CreatePost(authorId);
        _posts.GetByIdAsync(Arg.Any<ForumPostId>()).Returns(post);
        _currentUser.Id.Returns(authorId);
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new ForumDeletePostCommand(post.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _posts.Received(1).Remove(post);
        await _moderationLog.DidNotReceive().AddAsync(Arg.Any<ModerationLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModeratorDeletingOthersPost_RemovesAndLogsModeration()
    {
        var post = CreatePost(UserId.New());
        _posts.GetByIdAsync(Arg.Any<ForumPostId>()).Returns(post);
        var moderatorId = UserId.New();
        _currentUser.Id.Returns(moderatorId);
        _currentUser.Role.Returns(UserRole.Moderator);
        var handler = CreateHandler();

        var result = await handler.Handle(new ForumDeletePostCommand(post.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _posts.Received(1).Remove(post);
        await _moderationLog.Received(1).AddAsync(
            Arg.Is<ModerationLogEntry>(e => e.Action == "DeletePost" && e.ActorUserId == moderatorId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModeratorDeletingOwnPost_RemovesWithoutLoggingModeration()
    {
        var moderatorId = UserId.New();
        var post = CreatePost(moderatorId);
        _posts.GetByIdAsync(Arg.Any<ForumPostId>()).Returns(post);
        _currentUser.Id.Returns(moderatorId);
        _currentUser.Role.Returns(UserRole.Moderator);
        var handler = CreateHandler();

        var result = await handler.Handle(new ForumDeletePostCommand(post.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _moderationLog.DidNotReceive().AddAsync(Arg.Any<ModerationLogEntry>(), Arg.Any<CancellationToken>());
    }
}

public class DeleteTopicCommandHandlerTests
{
    private readonly IForumTopicRepository _topics = Substitute.For<IForumTopicRepository>();
    private readonly IModerationLogRepository _moderationLog = Substitute.For<IModerationLogRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private DeleteTopicCommandHandler CreateHandler() => new(_topics, _moderationLog, _currentUser, _unitOfWork);

    private static ForumTopic CreateTopic(UserId authorId) => ForumTopic.Create(ForumCategoryId.New(), authorId, "Ищу пати");

    [Fact]
    public async Task Handle_TopicNotFound_ReturnsNotFound()
    {
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns((ForumTopic?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteTopicCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnrelatedPlayer_ReturnsForbidden()
    {
        var topic = CreateTopic(UserId.New());
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns(topic);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteTopicCommand(topic.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        _topics.DidNotReceive().Remove(Arg.Any<ForumTopic>());
    }

    [Fact]
    public async Task Handle_Author_RemovesWithoutLoggingModeration()
    {
        var authorId = UserId.New();
        var topic = CreateTopic(authorId);
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns(topic);
        _currentUser.Id.Returns(authorId);
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteTopicCommand(topic.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _topics.Received(1).Remove(topic);
        await _moderationLog.DidNotReceive().AddAsync(Arg.Any<ModerationLogEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ModeratorDeletingOthersTopic_RemovesAndLogsModerationWithTitle()
    {
        var topic = CreateTopic(UserId.New());
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns(topic);
        var moderatorId = UserId.New();
        _currentUser.Id.Returns(moderatorId);
        _currentUser.Role.Returns(UserRole.Admin);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteTopicCommand(topic.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _topics.Received(1).Remove(topic);
        await _moderationLog.Received(1).AddAsync(
            Arg.Is<ModerationLogEntry>(e => e.Action == "DeleteTopic" && e.ActorUserId == moderatorId),
            Arg.Any<CancellationToken>());
    }
}

public class DiscussionDeletePostCommandHandlerTests
{
    private readonly IDiscussionRepository _repository = Substitute.For<IDiscussionRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private DiscussionDeletePostCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    private static DiscussionPost CreatePost(UserId authorId) =>
        DiscussionPost.Create(DiscussionEntityType.Spell, "some-slug", authorId, "Text");

    [Fact]
    public async Task Handle_PostNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<DiscussionPostId>()).Returns((DiscussionPost?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new DiscussionDeletePostCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnrelatedPlayer_ReturnsForbidden()
    {
        var post = CreatePost(UserId.New());
        _repository.GetByIdAsync(Arg.Any<DiscussionPostId>()).Returns(post);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DiscussionDeletePostCommand(post.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        _repository.DidNotReceive().Remove(Arg.Any<DiscussionPost>());
    }

    [Fact]
    public async Task Handle_Author_RemovesPost()
    {
        var authorId = UserId.New();
        var post = CreatePost(authorId);
        _repository.GetByIdAsync(Arg.Any<DiscussionPostId>()).Returns(post);
        _currentUser.Id.Returns(authorId);
        _currentUser.Role.Returns(UserRole.Player);
        var handler = CreateHandler();

        var result = await handler.Handle(new DiscussionDeletePostCommand(post.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Received(1).Remove(post);
    }

    [Fact]
    public async Task Handle_ModeratorDeletingOthersPost_Succeeds()
    {
        var post = CreatePost(UserId.New());
        _repository.GetByIdAsync(Arg.Any<DiscussionPostId>()).Returns(post);
        _currentUser.Id.Returns(UserId.New());
        _currentUser.Role.Returns(UserRole.Moderator);
        var handler = CreateHandler();

        var result = await handler.Handle(new DiscussionDeletePostCommand(post.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Received(1).Remove(post);
    }
}
