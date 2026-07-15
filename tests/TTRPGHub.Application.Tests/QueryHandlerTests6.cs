using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Campaigns.Queries.GetAllCampaigns;
using TTRPGHub.Features.Campaigns.Queries.GetCampaignDetail;
using TTRPGHub.Features.Campaigns.Queries.GetMyCampaigns;
using TTRPGHub.Features.Encounters.Queries.GetEncounterDetail;
using TTRPGHub.Features.Encounters.Queries.GetEncountersByCampaign;
using TTRPGHub.Features.Sessions.Queries.GetMySessions;
using TTRPGHub.Features.Sessions.Queries.GetSessionDetail;
using TTRPGHub.Features.Sessions.Queries.GetUpcomingSessions;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class GetEncounterDetailQueryHandlerTests
{
    private readonly IEncounterRepository _repository = Substitute.For<IEncounterRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetEncounterDetailQueryHandler CreateHandler() => new(_repository, _currentUser);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EncounterId>()).Returns((Encounter?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetEncounterDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MarksIsCreatorForOwnEncounter()
    {
        var creatorId = UserId.New();
        var encounter = Encounter.Create(CampaignId.New(), creatorId, "Ambush", null, EncounterDifficulty.Medium, null);
        _repository.GetByIdAsync(Arg.Any<EncounterId>()).Returns(encounter);
        _currentUser.Id.Returns(creatorId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetEncounterDetailQuery(encounter.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCreator);
    }
}

public class GetEncountersByCampaignQueryHandlerTests
{
    private readonly IEncounterRepository _repository = Substitute.For<IEncounterRepository>();

    private GetEncountersByCampaignQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_ReturnsEncountersForCampaign()
    {
        var campaignId = CampaignId.New();
        var encounter = Encounter.Create(campaignId, UserId.New(), "Ambush", null, EncounterDifficulty.Medium, null);
        _repository.GetByCampaignAsync(campaignId).Returns((IReadOnlyList<Encounter>)[encounter]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetEncountersByCampaignQuery(campaignId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class GetCampaignDetailQueryHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetCampaignDetailQueryHandler CreateHandler() => new(_repository, _userRepository, _currentUser);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns((Campaign?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCampaignDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_MarksOrganizerAndParticipantFlagsCorrectly()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _userRepository.GetByIdAsync(Arg.Any<UserId>()).Returns(
            User.Create("organizer", Email.Create("organizer@test.com").Value!, "hash"));
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCampaignDetailQuery(campaign.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCurrentUserOrganizer);
        Assert.True(result.Value.IsCurrentUserParticipant);
        Assert.Single(result.Value.Participants);
    }
}

public class GetAllCampaignsQueryHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();

    private GetAllCampaignsQueryHandler CreateHandler() => new(_repository);

    [Fact]
    public async Task Handle_ReturnsActiveCampaigns()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");
        _repository.GetActiveAsync().Returns((IReadOnlyList<Campaign>)[campaign]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetAllCampaignsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class GetMyCampaignsQueryHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetMyCampaignsQueryHandler CreateHandler() => new(_repository, _currentUser);

    [Fact]
    public async Task Handle_MarksIsOrganizerForOwnedCampaigns()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        _currentUser.Id.Returns(organizerId);
        _repository.GetByParticipantAsync(organizerId).Returns((IReadOnlyList<Campaign>)[campaign]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetMyCampaignsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value![0].IsOrganizer);
    }
}

public class GetSessionDetailQueryHandlerTests
{
    private readonly IGameSessionRepository _repository = Substitute.For<IGameSessionRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetSessionDetailQueryHandler CreateHandler() => new(_repository, _userRepository, _currentUser);

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns((GameSession?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSessionDetailQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ReturnsDetailWithOrganizerAndParticipantFlags()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _repository.GetByIdAsync(Arg.Any<GameSessionId>()).Returns(session);
        _userRepository.GetByIdAsync(Arg.Any<UserId>()).Returns(
            User.Create("organizer", Email.Create("organizer@test.com").Value!, "hash"));
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetSessionDetailQuery(session.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsCurrentUserOrganizer);
        Assert.True(result.Value.IsCurrentUserParticipant);
    }
}

public class GetMySessionsQueryHandlerTests
{
    private readonly IGameSessionRepository _repository = Substitute.For<IGameSessionRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetMySessionsQueryHandler CreateHandler() => new(_repository, _userRepository, _currentUser);

    [Fact]
    public async Task Handle_DeduplicatesSessionsWhereUserIsBothOrganizerAndParticipant()
    {
        var userId = UserId.New();
        _currentUser.Id.Returns(userId);
        var session = GameSession.Create(userId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _repository.GetByParticipantAsync(userId).Returns((IReadOnlyList<GameSession>)[session]);
        _repository.GetByOrganizerAsync(userId).Returns((IReadOnlyList<GameSession>)[session]);
        _userRepository.GetByIdAsync(Arg.Any<UserId>()).Returns(
            User.Create("organizer", Email.Create("organizer@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetMySessionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}

public class GetUpcomingSessionsQueryHandlerTests
{
    private readonly IGameSessionRepository _repository = Substitute.For<IGameSessionRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();

    private GetUpcomingSessionsQueryHandler CreateHandler() => new(_repository, _userRepository);

    [Fact]
    public async Task Handle_ReturnsSummariesWithOrganizerName()
    {
        var organizerId = UserId.New();
        var session = GameSession.Create(organizerId, "Test", null, "pf2e", 4, DateTime.UtcNow.AddDays(1), SessionFormat.Online, null);
        _repository.GetUpcomingAsync(1, 20, null, null).Returns((IReadOnlyList<GameSession>)[session]);
        _userRepository.GetByIdAsync(organizerId).Returns(
            User.Create("organizer", Email.Create("organizer@test.com").Value!, "hash"));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetUpcomingSessionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("organizer", result.Value![0].OrganizerName);
    }
}
