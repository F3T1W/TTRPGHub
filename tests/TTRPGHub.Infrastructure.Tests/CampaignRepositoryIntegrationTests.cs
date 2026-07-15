using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Infrastructure.Tests;

[Collection("Postgres")]
public class CampaignRepositoryIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_IncludesAutoCreatedOrganizerParticipant()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CampaignRepository(db);
        var organizerId = UserId.New();
        var campaign = Campaign.Create(organizerId, "Curse of the Crimson Throne", null, "pf2e");

        await repository.AddAsync(campaign);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var fetched = await new CampaignRepository(readDb).GetByIdAsync(campaign.Id);
        Assert.NotNull(fetched);
        Assert.Single(fetched!.Participants);
        Assert.Equal(organizerId, fetched.Participants[0].UserId);
    }

    [Fact]
    public async Task GetByParticipantAsync_FindsCampaignsWherePlayerWasAdded()
    {
        // OwnsMany navigation through a real subquery, same class of regression as
        // GameSession.Participants — worth the real-DB check.
        await using var db = fixture.CreateDbContext();
        var repository = new CampaignRepository(db);
        var playerId = UserId.New();
        var campaign = Campaign.Create(UserId.New(), "Test Campaign", null, "pf2e");
        campaign.AddParticipant(playerId);
        await repository.AddAsync(campaign);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new CampaignRepository(readDb).GetByParticipantAsync(playerId);

        Assert.Single(results);
        Assert.Equal(campaign.Id, results[0].Id);
    }

    [Fact]
    public async Task GetByOrganizerAsync_ExcludesCampaignsOrganizedBySomeoneElse()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CampaignRepository(db);
        var organizerId = UserId.New();
        await repository.AddAsync(Campaign.Create(organizerId, "Mine", null, "pf2e"));
        await repository.AddAsync(Campaign.Create(UserId.New(), "Theirs", null, "pf2e"));
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new CampaignRepository(readDb).GetByOrganizerAsync(organizerId);

        Assert.Single(results);
        Assert.Equal("Mine", results[0].Title);
    }

    [Fact]
    public async Task GetActiveAsync_ExcludesArchivedCampaigns()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CampaignRepository(db);
        var active = Campaign.Create(UserId.New(), $"Active {Guid.NewGuid():N}", null, "pf2e");
        var archived = Campaign.Create(UserId.New(), $"Archived {Guid.NewGuid():N}", null, "pf2e");
        archived.ChangeStatus(CampaignStatus.Archived);
        await repository.AddAsync(active);
        await repository.AddAsync(archived);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new CampaignRepository(readDb).GetActiveAsync();

        Assert.Contains(results, c => c.Id == active.Id);
        Assert.DoesNotContain(results, c => c.Id == archived.Id);
    }

    [Fact]
    public async Task Update_PersistsParticipantRemoval()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CampaignRepository(db);
        var playerId = UserId.New();
        var campaign = Campaign.Create(UserId.New(), "Test", null, "pf2e");
        campaign.AddParticipant(playerId);
        await repository.AddAsync(campaign);
        await db.SaveChangesAsync();

        await using var updateDb = fixture.CreateDbContext();
        var updateRepo = new CampaignRepository(updateDb);
        var toUpdate = await updateRepo.GetByIdAsync(campaign.Id);
        toUpdate!.RemoveParticipant(playerId);
        updateRepo.Update(toUpdate);
        await updateDb.SaveChangesAsync();

        await using var verifyDb = fixture.CreateDbContext();
        var verified = await new CampaignRepository(verifyDb).GetByIdAsync(campaign.Id);
        Assert.DoesNotContain(verified!.Participants, p => p.UserId == playerId);
    }
}
