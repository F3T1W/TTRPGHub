using TTRPGHub.Entities;

namespace TTRPGHub.Domain.Tests;

public class CampaignTests
{
    [Fact]
    public void Create_AutomaticallyAddsOrganizerAsDungeonMasterParticipant()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Curse of the Crimson Throne", null, "pf2e");

        Assert.Single(campaign.Participants);
        Assert.Equal(organizerId, campaign.Participants[0].UserId);
        Assert.Equal(CampaignRole.DungeonMaster, campaign.Participants[0].Role);
    }

    [Fact]
    public void Create_StartsActive()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");

        Assert.Equal(CampaignStatus.Active, campaign.Status);
    }

    [Fact]
    public void AddParticipant_NewUser_AddsAsPlayer()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");
        var playerId = UserId.New();

        var result = campaign.AddParticipant(playerId);

        Assert.True(result.IsSuccess);
        Assert.Contains(campaign.Participants, p => p.UserId == playerId && p.Role == CampaignRole.Player);
    }

    [Fact]
    public void AddParticipant_AlreadyParticipant_ReturnsConflict()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");

        var result = campaign.AddParticipant(organizerId);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RemoveParticipant_Organizer_ReturnsValidationError()
    {
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Test", null, "pf2e");

        var result = campaign.RemoveParticipant(organizerId);

        Assert.True(result.IsFailure);
        Assert.Contains(campaign.Participants, p => p.UserId == organizerId);
    }

    [Fact]
    public void RemoveParticipant_NotAParticipant_ReturnsNotFound()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");

        var result = campaign.RemoveParticipant(UserId.New());

        Assert.True(result.IsFailure);
    }

    [Fact]
    public void RemoveParticipant_ExistingPlayer_RemovesThem()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");
        var playerId = UserId.New();
        campaign.AddParticipant(playerId);

        var result = campaign.RemoveParticipant(playerId);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(campaign.Participants, p => p.UserId == playerId);
    }

    [Fact]
    public void ChangeStatus_UpdatesStatus()
    {
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");

        campaign.ChangeStatus(CampaignStatus.Archived);

        Assert.Equal(CampaignStatus.Archived, campaign.Status);
    }
}
