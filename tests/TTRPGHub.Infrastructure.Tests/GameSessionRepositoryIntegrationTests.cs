using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Infrastructure.Tests;

[Collection("Postgres")]
public class GameSessionRepositoryIntegrationTests(PostgresFixture fixture)
{
    private static GameSession CreateSession(
        UserId organizerId, string? location = null, SessionFormat format = SessionFormat.Online, DateTime? scheduledAt = null) =>
        GameSession.Create(
            organizerId, $"Session {Guid.NewGuid():N}", null, "pf2e", 4,
            scheduledAt ?? DateTime.UtcNow.AddDays(1), format, location);

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_IncludesParticipants()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new GameSessionRepository(db);
        var organizerId = UserId.New();
        var playerId = UserId.New();
        var session = CreateSession(organizerId);
        session.Join(playerId);

        await repository.AddAsync(session);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var fetched = await new GameSessionRepository(readDb).GetByIdAsync(session.Id);
        Assert.NotNull(fetched);
        Assert.Equal(2, fetched!.Participants.Count);
    }

    [Fact]
    public async Task GetByOrganizerAsync_ReturnsOnlySessionsTheyOrganize()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new GameSessionRepository(db);
        var organizerId = UserId.New();
        await repository.AddAsync(CreateSession(organizerId));
        await repository.AddAsync(CreateSession(UserId.New()));
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new GameSessionRepository(readDb).GetByOrganizerAsync(organizerId);

        Assert.Single(results);
        Assert.All(results, s => Assert.Equal(organizerId, s.OrganizerId));
    }

    [Fact]
    public async Task GetByParticipantAsync_FindsSessionsWhereUserJoinedButDidNotOrganize()
    {
        // Real invariant worth locking against a real DB: Participants is an EF owned collection
        // (OwnsMany), and `.Any(p => p.UserId == userId)` must translate to a correlated subquery
        // against the campaign_participants-style child table — not something InMemory validates.
        await using var db = fixture.CreateDbContext();
        var repository = new GameSessionRepository(db);
        var playerId = UserId.New();
        var session = CreateSession(UserId.New());
        session.Join(playerId);
        await repository.AddAsync(session);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new GameSessionRepository(readDb).GetByParticipantAsync(playerId);

        Assert.Single(results);
        Assert.Equal(session.Id, results[0].Id);
    }

    [Fact]
    public async Task GetUpcomingAsync_ExcludesPastAndNonPlannedSessions()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new GameSessionRepository(db);
        var marker = $"upcoming-marker-{Guid.NewGuid():N}";
        var organizerId = UserId.New();
        var upcoming = CreateSession(organizerId, location: marker, scheduledAt: DateTime.UtcNow.AddDays(5));
        var past = CreateSession(organizerId, location: marker, scheduledAt: DateTime.UtcNow.AddDays(5));
        past.Start(organizerId);
        past.Complete(organizerId);
        await repository.AddAsync(upcoming);
        await repository.AddAsync(past);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new GameSessionRepository(readDb).GetUpcomingAsync(1, 10, location: marker);

        Assert.Single(results);
        Assert.Equal(upcoming.Id, results[0].Id);
    }

    [Fact]
    public async Task GetUpcomingAsync_FiltersByFormat()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new GameSessionRepository(db);
        var marker = $"format-marker-{Guid.NewGuid():N}";
        await repository.AddAsync(CreateSession(UserId.New(), location: marker, format: SessionFormat.Online));
        await repository.AddAsync(CreateSession(UserId.New(), location: marker, format: SessionFormat.Offline));
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new GameSessionRepository(readDb).GetUpcomingAsync(1, 10, location: marker, format: SessionFormat.Offline);

        Assert.Single(results);
        Assert.Equal(SessionFormat.Offline, results[0].Format);
    }

    [Fact]
    public async Task GetUpcomingAsync_LocationFilterIsCaseInsensitive()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new GameSessionRepository(db);
        var marker = $"CaseMarker{Guid.NewGuid():N}";
        await repository.AddAsync(CreateSession(UserId.New(), location: marker));
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new GameSessionRepository(readDb).GetUpcomingAsync(1, 10, location: marker.ToLowerInvariant());

        Assert.Single(results);
    }
}
