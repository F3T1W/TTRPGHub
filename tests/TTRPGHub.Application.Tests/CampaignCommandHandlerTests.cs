using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Campaigns.Commands.AddParticipant;
using TTRPGHub.Features.Campaigns.Commands.ChangeCampaignStatus;
using TTRPGHub.Features.Campaigns.Commands.CreateCampaign;
using TTRPGHub.Features.Campaigns.Commands.RemoveParticipant;
using TTRPGHub.Features.Campaigns.Commands.UpdateCampaign;
using TTRPGHub.Repositories;

namespace TTRPGHub.Application.Tests;

public class CreateCampaignCommandHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private CreateCampaignCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    [Fact]
    public async Task Handle_CreatesCampaignOwnedByCurrentUserAsOrganizer()
    {
        var organizerId = UserId.New();
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new CreateCampaignCommand("Curse of the Crimson Throne", "Long saga", "pf2e"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).AddAsync(
            Arg.Is<Campaign>(c => c.OrganizerId == organizerId && c.Status == CampaignStatus.Active),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AutomaticallyAddsOrganizerAsParticipant()
    {
        var organizerId = UserId.New();
        _currentUser.Id.Returns(organizerId);
        Campaign? captured = null;
        _repository.When(r => r.AddAsync(Arg.Any<Campaign>(), Arg.Any<CancellationToken>()))
            .Do(call => captured = call.Arg<Campaign>());
        var handler = CreateHandler();

        await handler.Handle(new CreateCampaignCommand("Test", null, "pf2e"), CancellationToken.None);

        Assert.Single(captured!.Participants);
        Assert.Equal(organizerId, captured.Participants[0].UserId);
    }
}

public class UpdateCampaignCommandHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private UpdateCampaignCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    [Fact]
    public async Task Handle_CampaignNotFound_ReturnsNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns((Campaign?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCampaignCommand(Guid.NewGuid(), "New title", null, "pf2e"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_NonOrganizer_ReturnsUnauthorized()
    {
        var campaign = Campaign.Create(UserId.New(), "Old title", null, "pf2e");
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCampaignCommand(campaign.Id.Value, "New title", null, "pf2e"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Old title", campaign.Title);
    }

    [Fact]
    public async Task Handle_Organizer_UpdatesCampaign()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Old title", null, "pf2e");
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new UpdateCampaignCommand(campaign.Id.Value, "New title", "New description", "5e"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("New title", campaign.Title);
        Assert.Equal("New description", campaign.Description);
        Assert.Equal("5e", campaign.System);
    }
}

public class ChangeCampaignStatusCommandHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private ChangeCampaignStatusCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    [Fact]
    public async Task Handle_NonOrganizer_ReturnsUnauthorized()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangeCampaignStatusCommand(campaign.Id.Value, CampaignStatus.Archived), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(CampaignStatus.Active, campaign.Status);
    }

    [Fact]
    public async Task Handle_Organizer_ChangesStatus()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new ChangeCampaignStatusCommand(campaign.Id.Value, CampaignStatus.Paused), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(CampaignStatus.Paused, campaign.Status);
    }
}

public class AddParticipantCommandHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private AddParticipantCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    [Fact]
    public async Task Handle_NonOrganizer_ReturnsUnauthorized()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AddParticipantCommand(campaign.Id.Value, UserId.New().Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_AlreadyParticipant_ReturnsConflict()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AddParticipantCommand(campaign.Id.Value, organizerId.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_Organizer_AddsNewParticipant()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(organizerId);
        var newPlayerId = UserId.New();
        var handler = CreateHandler();

        var result = await handler.Handle(
            new AddParticipantCommand(campaign.Id.Value, newPlayerId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, campaign.Participants.Count);
        Assert.Contains(campaign.Participants, p => p.UserId == newPlayerId);
    }
}

public class RemoveParticipantCommandHandlerTests
{
    private readonly ICampaignRepository _repository = Substitute.For<ICampaignRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private RemoveParticipantCommandHandler CreateHandler() => new(_repository, _unitOfWork, _currentUser);

    private static Campaign CreateCampaignWithPlayer(out UserId organizerId, out UserId playerId)
    {
        organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");
        playerId = UserId.New();
        campaign.AddParticipant(playerId);
        return campaign;
    }

    [Fact]
    public async Task Handle_StrangerRemovingSomeoneElse_ReturnsUnauthorized()
    {
        var campaign = CreateCampaignWithPlayer(out _, out var playerId);
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(UserId.New());
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RemoveParticipantCommand(campaign.Id.Value, playerId.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_PlayerRemovesSelf_Succeeds()
    {
        var campaign = CreateCampaignWithPlayer(out _, out var playerId);
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(playerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RemoveParticipantCommand(campaign.Id.Value, playerId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(campaign.Participants, p => p.UserId == playerId);
    }

    [Fact]
    public async Task Handle_OrganizerRemovesOtherPlayer_Succeeds()
    {
        var campaign = CreateCampaignWithPlayer(out var organizerId, out var playerId);
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RemoveParticipantCommand(campaign.Id.Value, playerId.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(campaign.Participants, p => p.UserId == playerId);
    }

    [Fact]
    public async Task Handle_OrganizerCannotRemoveSelf_ReturnsValidationError()
    {
        var campaign = CreateCampaignWithPlayer(out var organizerId, out _);
        _repository.GetByIdAsync(Arg.Any<CampaignId>()).Returns(campaign);
        _currentUser.Id.Returns(organizerId);
        var handler = CreateHandler();

        var result = await handler.Handle(
            new RemoveParticipantCommand(campaign.Id.Value, organizerId.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Contains(campaign.Participants, p => p.UserId == organizerId);
    }
}
