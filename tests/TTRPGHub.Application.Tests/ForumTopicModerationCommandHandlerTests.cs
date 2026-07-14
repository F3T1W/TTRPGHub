using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Features.Forum.Commands.SetTopicLocked;
using TTRPGHub.Features.Forum.Commands.SetTopicPinned;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Forum;

namespace TTRPGHub.Application.Tests;

public class SetTopicLockedCommandHandlerTests
{
    private readonly IForumTopicRepository _topics = Substitute.For<IForumTopicRepository>();
    private readonly IModerationLogRepository _moderationLog = Substitute.For<IModerationLogRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SetTopicLockedCommandHandler CreateHandler() => new(_topics, _moderationLog, _currentUser, _unitOfWork);

    private static ForumTopic CreateTopic() => ForumTopic.Create(ForumCategoryId.New(), UserId.New(), "Ищу пати");

    [Fact]
    public async Task Handle_TopicNotFound_ReturnsNotFound()
    {
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns((ForumTopic?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new SetTopicLockedCommand(Guid.NewGuid(), true), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Lock_SetsIsLockedAndLogsLockAction()
    {
        var topic = CreateTopic();
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns(topic);
        var moderatorId = UserId.New();
        _currentUser.Id.Returns(moderatorId);
        var handler = CreateHandler();

        var result = await handler.Handle(new SetTopicLockedCommand(topic.Id.Value, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(topic.IsLocked);
        await _moderationLog.Received(1).AddAsync(
            Arg.Is<ModerationLogEntry>(e => e.Action == "LockTopic" && e.ActorUserId == moderatorId),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Unlock_ClearsIsLockedAndLogsUnlockAction()
    {
        var topic = CreateTopic();
        topic.Lock();
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns(topic);
        var handler = CreateHandler();

        var result = await handler.Handle(new SetTopicLockedCommand(topic.Id.Value, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(topic.IsLocked);
        await _moderationLog.Received(1).AddAsync(
            Arg.Is<ModerationLogEntry>(e => e.Action == "UnlockTopic"), Arg.Any<CancellationToken>());
    }
}

public class SetTopicPinnedCommandHandlerTests
{
    private readonly IForumTopicRepository _topics = Substitute.For<IForumTopicRepository>();
    private readonly IModerationLogRepository _moderationLog = Substitute.For<IModerationLogRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    private SetTopicPinnedCommandHandler CreateHandler() => new(_topics, _moderationLog, _currentUser, _unitOfWork);

    private static ForumTopic CreateTopic() => ForumTopic.Create(ForumCategoryId.New(), UserId.New(), "Ищу пати");

    [Fact]
    public async Task Handle_TopicNotFound_ReturnsNotFound()
    {
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns((ForumTopic?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new SetTopicPinnedCommand(Guid.NewGuid(), true), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Pin_SetsIsPinnedAndLogsPinAction()
    {
        var topic = CreateTopic();
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns(topic);
        var handler = CreateHandler();

        var result = await handler.Handle(new SetTopicPinnedCommand(topic.Id.Value, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(topic.IsPinned);
        await _moderationLog.Received(1).AddAsync(
            Arg.Is<ModerationLogEntry>(e => e.Action == "PinTopic"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Unpin_ClearsIsPinnedAndLogsUnpinAction()
    {
        var topic = CreateTopic();
        topic.Pin();
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns(topic);
        var handler = CreateHandler();

        var result = await handler.Handle(new SetTopicPinnedCommand(topic.Id.Value, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(topic.IsPinned);
        await _moderationLog.Received(1).AddAsync(
            Arg.Is<ModerationLogEntry>(e => e.Action == "UnpinTopic"), Arg.Any<CancellationToken>());
    }
}
