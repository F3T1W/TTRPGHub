using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Encounters.Commands.CreateEncounter;
using TTRPGHub.Features.Encounters.Commands.DeleteEncounter;
using TTRPGHub.Features.Encounters.Commands.UpdateEncounter;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateEncounterCommandHandlerTests
{
    private readonly IEncounterRepository _repository = Substitute.For<IEncounterRepository>();
    private readonly ICampaignRepository _campaignRepository = Substitute.For<ICampaignRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private CreateEncounterCommandHandler CreateHandler() =>
        new(_repository, _campaignRepository, _unitOfWork, _currentUser);

    private static CreateEncounterCommand ValidCommand(Guid campaignId) => new(
        campaignId, "Ambush at the bridge", "Goblins attack from both sides",
        EncounterDifficulty.Medium, null, [new EncounterEntryInput("Goblin Warrior", 4, null)]);

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        _campaignRepository.GetByIdAsync(Arg.Any<CampaignId>()).Returns((Campaign?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonParticipant_ReturnsUnauthorized()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");
        _campaignRepository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(campaign.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _repository.DidNotReceive().AddAsync(Arg.Any<Encounter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Organizer_CreatesEncounterWithEntries()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        _campaignRepository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(campaign.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).AddAsync(
            Arg.Is<Encounter>(e => e.CreatedById == organizerId && e.CampaignId == campaign.Id
                && e.Entries.Count == 1 && e.Entries[0].Name == "Goblin Warrior"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RegularParticipant_CanAlsoCreateEncounter()
    {
        var organizerId = UserId.New();
        var participantId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        campaign.AddParticipant(participantId);
        _campaignRepository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(participantId);
        var handler = CreateHandler();

        var result = await handler.Handle(ValidCommand(campaign.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}

public class UpdateEncounterCommandHandlerTests
{
    private readonly IEncounterRepository _repository = Substitute.For<IEncounterRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private UpdateEncounterCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    private static Encounter CreateEncounter(UserId createdById) => Encounter.Create(
        CampaignId.New(), createdById, "Old title", null, EncounterDifficulty.Easy, null);

    [Fact]
    public async Task Handle_EncounterNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<EncounterId>()).Returns((Encounter?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateEncounterCommand(Guid.NewGuid(), "New title", null, EncounterDifficulty.Hard, null, []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonCreator_ReturnsUnauthorized()
    {
        var encounter = CreateEncounter(UserId.New());
        _repository.GetByIdAsync(Arg.Any<EncounterId>()).Returns(encounter);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateEncounterCommand(encounter.Id.Value, "New title", null, EncounterDifficulty.Hard, null, []),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Old title", encounter.Title);
    }

    [Fact]
    public async Task Handle_Creator_UpdatesEncounterAndReplacesEntries()
    {
        var creatorId = UserId.New();
        var encounter = CreateEncounter(creatorId);
        encounter.SetEntries([new EncounterEntry { Name = "Old Monster", Count = 1 }]);
        _repository.GetByIdAsync(Arg.Any<EncounterId>()).Returns(encounter);
        _currentUser.Id.Returns(creatorId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateEncounterCommand(encounter.Id.Value, "New title", "New description", EncounterDifficulty.Deadly, "Beware traps",
                [new EncounterEntryInput("Owlbear", 2, null)]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New title", encounter.Title);
        Assert.Equal(EncounterDifficulty.Deadly, encounter.Difficulty);
        Assert.Single(encounter.Entries);
        Assert.Equal("Owlbear", encounter.Entries[0].Name);
        _repository.Received(1).Update(encounter);
    }
}

public class DeleteEncounterCommandHandlerTests
{
    private readonly IEncounterRepository _repository = Substitute.For<IEncounterRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private DeleteEncounterCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    private static Encounter CreateEncounter(UserId createdById) => Encounter.Create(
        CampaignId.New(), createdById, "Test", null, EncounterDifficulty.Easy, null);

    [Fact]
    public async Task Handle_NonCreator_ReturnsUnauthorized()
    {
        var encounter = CreateEncounter(UserId.New());
        _repository.GetByIdAsync(Arg.Any<EncounterId>()).Returns(encounter);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteEncounterCommand(encounter.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        _repository.DidNotReceive().Delete(Arg.Any<Encounter>());
    }

    [Fact]
    public async Task Handle_Creator_DeletesEncounter()
    {
        var creatorId = UserId.New();
        var encounter = CreateEncounter(creatorId);
        _repository.GetByIdAsync(Arg.Any<EncounterId>()).Returns(encounter);
        _currentUser.Id.Returns(creatorId);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteEncounterCommand(encounter.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _repository.Received(1).Delete(encounter);
    }
}
