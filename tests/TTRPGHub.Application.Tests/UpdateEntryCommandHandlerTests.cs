using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Initiative.Commands.UpdateEntry;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class UpdateEntryCommandHandlerTests
{
    private readonly IInitiativeTrackerRepository _repository = Substitute.For<IInitiativeTrackerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ITrackerNotifier _notifier = Substitute.For<ITrackerNotifier>();
    private readonly ITableTokenRepository _tokenRepository = Substitute.For<ITableTokenRepository>();
    private readonly ITableNotifier _tableNotifier = Substitute.For<ITableNotifier>();

    private UpdateEntryCommandHandler CreateHandler() => new(
        _repository, _unitOfWork, _currentUser, _notifier,
        TestDoubles.CreateInertTrackerSync(tokenRepository: _tokenRepository, tableNotifier: _tableNotifier));

    [Fact]
    public async Task Handle_TrackerNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<InitiativeTrackerId>()).Returns((InitiativeTracker?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateEntryCommand(Guid.NewGuid(), Guid.NewGuid(), 5, EntryStatus.Active, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsUnauthorized()
    {
        var tracker = InitiativeTracker.Create(CampaignId.New(), UserId.New(), "Fight");
        _repository.GetByIdAsync(Arg.Any<InitiativeTrackerId>()).Returns(tracker);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateEntryCommand(Guid.NewGuid(), Guid.NewGuid(), 5, EntryStatus.Active, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Owner_UpdatesEntry()
    {
        var ownerId = UserId.New();
        var tracker = InitiativeTracker.Create(CampaignId.New(), ownerId, "Fight");
        tracker.SyncFromToken(Guid.NewGuid(), "Goblin", 10, 8, 8, 14, null, false);
        var entryId = tracker.Entries.Single().Id;
        _repository.GetByIdAsync(Arg.Any<InitiativeTrackerId>()).Returns(tracker);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateEntryCommand(Guid.NewGuid(), entryId, 3, EntryStatus.Unconscious, "on the ropes"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var entry = tracker.Entries.Single();
        Assert.Equal(3, entry.CurrentHp);
        Assert.Equal(EntryStatus.Unconscious, entry.Status);
        Assert.Equal("on the ropes", entry.Notes);
    }

    [Fact]
    public async Task Handle_LinkedSessionAndLinkedToken_PushesHpToTableToken()
    {
        var ownerId = UserId.New();
        var tracker = InitiativeTracker.Create(CampaignId.New(), ownerId, "Fight");
        var sessionId = Guid.NewGuid();
        tracker.LinkSession(sessionId);
        var linkedTokenId = Guid.NewGuid();
        tracker.SyncFromToken(linkedTokenId, "Goblin", 10, 8, 8, 14, null, false);
        var entryId = tracker.Entries.Single().Id;
        var token = TableToken.Create(new GameSessionId(sessionId), Guid.NewGuid(), "Goblin", null, "#f00", 1, 1, null,
            currentHp: 8, maxHp: 8, armorClass: 14);
        _repository.GetByIdAsync(Arg.Any<InitiativeTrackerId>()).Returns(tracker);
        _currentUser.Id.Returns(ownerId);
        _tokenRepository.GetByIdAsync(linkedTokenId).Returns(token);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateEntryCommand(Guid.NewGuid(), entryId, 2, EntryStatus.Unconscious, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, token.CurrentHp);
    }

    [Fact]
    public async Task Handle_NoLinkedSession_DoesNotTouchTableTokens()
    {
        var ownerId = UserId.New();
        var tracker = InitiativeTracker.Create(CampaignId.New(), ownerId, "Fight");
        var linkedTokenId = Guid.NewGuid();
        tracker.SyncFromToken(linkedTokenId, "Goblin", 10, 8, 8, 14, null, false);
        var entryId = tracker.Entries.Single().Id;
        _repository.GetByIdAsync(Arg.Any<InitiativeTrackerId>()).Returns(tracker);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new UpdateEntryCommand(Guid.NewGuid(), entryId, 2, EntryStatus.Unconscious, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _tokenRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }
}
