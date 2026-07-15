using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Events;
using TTRPGHub.Features.Characters.Queries.GetMyCharacters;
using TTRPGHub.Features.Events.Queries.GetEventDetail;
using TTRPGHub.Features.Events.Queries.GetEvents;
using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;
using TTRPGHub.Features.Initiative.Queries.GetTrackersByCampaign;
using TTRPGHub.Features.Macros.Queries.GetMyMacros;
using TTRPGHub.Features.Users.Queries.GetAllUsers;
using TTRPGHub.Features.Users.Queries.GetUserProfile;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class GetMyCharactersQueryHandlerTests
{
    private readonly ICharacterRepository _characterRepository = Substitute.For<ICharacterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly ICacheService _cache = Substitute.For<ICacheService>();

    private GetMyCharactersQueryHandler CreateHandler() => new(_characterRepository, _currentUser, _cache);

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedResultWithoutHittingRepository()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var cached = new List<CharacterSummaryDto> { new(Guid.NewGuid(), "Grog", "Human", "Fighter", 1, null, DateTime.UtcNow) };
        _cache.GetAsync<IReadOnlyList<CharacterSummaryDto>>($"characters:owner:{userId.Value}").Returns(cached);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyCharactersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        await _characterRepository.DidNotReceive().GetByOwnerAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CacheMiss_FetchesAndCachesResult()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        _cache.GetAsync<IReadOnlyList<CharacterSummaryDto>>(Arg.Any<string>()).Returns((IReadOnlyList<CharacterSummaryDto>?)null);
        var character = Character.Create(userId, "Grog", "Human", "Fighter", 1).Value!;
        _characterRepository.GetByOwnerAsync(userId).Returns((IReadOnlyList<Character>)[character]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyCharactersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        await _cache.Received(1).SetAsync(
            $"characters:owner:{userId.Value}", Arg.Any<IReadOnlyList<CharacterSummaryDto>>(),
            Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>());
    }
}

public class GetEventsQueryHandlerTests
{
    private readonly IGameEventRepository _repo = Substitute.For<IGameEventRepository>();

    private GetEventsQueryHandler CreateHandler() => new(_repo);

    [Fact]
    public async Task Handle_ReturnsPagedEventSummaries()
    {
        var ev = GameEvent.Create(UserId.New(), "Open table", null, "pf2e", EventFormat.Online, null, null, DateTime.UtcNow.AddDays(1), 4);
        _repo.GetUpcomingAsync(1, 20, null, null).Returns([ev]);
        _repo.CountUpcomingAsync(null, null).Returns(1);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetEventsQuery(1, 20), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.Total);
    }
}

public class GetEventDetailQueryHandlerTests
{
    private readonly IGameEventRepository _repo = Substitute.For<IGameEventRepository>();

    private GetEventDetailQueryHandler CreateHandler() => new(_repo);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns((GameEvent?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetEventDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ReturnsDetailWithParticipants()
    {
        var ev = GameEvent.Create(UserId.New(), "Open table", "Bring a level 1 character", "pf2e", EventFormat.Online, null, null, DateTime.UtcNow.AddDays(1), 4);
        ev.AddParticipant(UserId.New());
        _repo.GetByIdWithParticipantsAsync(Arg.Any<GameEventId>()).Returns(ev);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetEventDetailQuery(ev.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Participants);
    }
}

public class GetTrackerDetailQueryHandlerTests
{
    private readonly IInitiativeTrackerRepository _repository = Substitute.For<IInitiativeTrackerRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetTrackerDetailQueryHandler CreateHandler() => new(_repository, _currentUser);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<InitiativeTrackerId>()).Returns((InitiativeTracker?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTrackerDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MarksIsOwnerForTrackerOwner()
    {
        var ownerId = UserId.New();
        var tracker = InitiativeTracker.Create(CampaignId.New(), ownerId, "Boss fight");
        _repository.GetByIdAsync(Arg.Any<InitiativeTrackerId>()).Returns(tracker);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTrackerDetailQuery(tracker.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsOwner);
    }
}

public class GetTrackersByCampaignQueryHandlerTests
{
    private readonly IInitiativeTrackerRepository _repository = Substitute.For<IInitiativeTrackerRepository>();

    private GetTrackersByCampaignQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_ReturnsTrackersForCampaign()
    {
        var campaignId = CampaignId.New();
        var tracker = InitiativeTracker.Create(campaignId, UserId.New(), "Boss fight");
        _repository.GetByCampaignAsync(campaignId).Returns((IReadOnlyList<InitiativeTracker>)[tracker]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTrackersByCampaignQuery(campaignId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class GetMyMacrosQueryHandlerTests
{
    private readonly IMacroRepository _macroRepository = Substitute.For<IMacroRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetMyMacrosQueryHandler CreateHandler() => new(_macroRepository, _currentUser);

    [Fact]
    public async Task Handle_ReturnsOwnedMacros()
    {
        var ownerId = UserId.New();
        _currentUser.Id.Returns(ownerId);
        var macro = Macro.Create(ownerId, "Fireball", null, MacroType.Chat, "/roll 8d6");
        _macroRepository.GetByOwnerAsync(ownerId).Returns((IReadOnlyList<Macro>)[macro]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyMacrosQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class GetUserProfileQueryHandlerTests
{
    private readonly IUserRepository _userRepo = Substitute.For<IUserRepository>();
    private readonly ICharacterRepository _characterRepo = Substitute.For<ICharacterRepository>();
    private readonly ICampaignRepository _campaignRepo = Substitute.For<ICampaignRepository>();

    private GetUserProfileQueryHandler CreateHandler() => new(_userRepo, _characterRepo, _campaignRepo);

    [Fact]
    public async Task Handle_UserNotFound_ReturnsNotFound()
    {
        _userRepo.GetByIdAsync(Arg.Any<UserId>()).Returns((User?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserProfileQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OnlyIncludesPublicCharacters()
    {
        var user = User.Create("gm", Email.Create("gm@test.com").Value!, "hash");
        _userRepo.GetByIdAsync(Arg.Any<UserId>()).Returns(user);
        var publicChar = Character.Create(user.Id, "Public Hero", "Human", "Fighter", 1).Value!;
        publicChar.SetPublic(true);
        var privateChar = Character.Create(user.Id, "Private Hero", "Human", "Fighter", 1).Value!;
        _characterRepo.GetByOwnerAsync(user.Id).Returns((IReadOnlyList<Character>)[publicChar, privateChar]);
        _campaignRepo.GetByOrganizerAsync(user.Id).Returns((IReadOnlyList<Campaign>)[]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUserProfileQuery(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Characters);
        Assert.Equal("Public Hero", result.Value.Characters[0].Name);
    }
}

public class GetAllUsersQueryHandlerTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();

    private GetAllUsersQueryHandler CreateHandler() => new(_users);

    [Fact]
    public async Task Handle_ReturnsPagedAdminUserList()
    {
        var user = User.Create("gm", Email.Create("gm@test.com").Value!, "hash");
        _users.SearchAsync(null, 1, 30).Returns((new List<User> { user }, 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetAllUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(1, result.Value.Total);
    }
}
