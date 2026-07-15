using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Infrastructure.Tests;

[Collection("Postgres")]
public class RuleEntryRepositoryIntegrationTests(PostgresFixture fixture)
{
    private async Task<GameSystem> CreateSystemAsync(AppDbContext db)
    {
        var system = GameSystem.CreateCustom($"system-{Guid.NewGuid():N}", "Test System", UserId.New());
        await new GameSystemRepository(db).AddAsync(system);
        await db.SaveChangesAsync();
        return system;
    }

    private static RuleEntry CreateEntry(GameSystemId systemId, string title, RuleCategory category = RuleCategory.Class) =>
        RuleEntry.Create(systemId, category, $"{title.ToLowerInvariant()}-{Guid.NewGuid():N}", title,
            null, null, "{}", [], isHomebrew: true, source: "Test");

    [Fact]
    public async Task AddAsync_ThenGetBySlugAsync_RoundTripsEntry()
    {
        await using var db = fixture.CreateDbContext();
        var system = await CreateSystemAsync(db);
        var repository = new RuleEntryRepository(db);
        var entry = CreateEntry(system.Id, "Gunslinger");

        await repository.AddAsync(entry);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var fetched = await new RuleEntryRepository(readDb).GetBySlugAsync(system.Id, RuleCategory.Class, entry.Slug);
        Assert.NotNull(fetched);
        Assert.Equal("Gunslinger", fetched!.Title);
    }

    [Fact]
    public async Task SearchAsync_FiltersByTitleCaseInsensitively()
    {
        await using var db = fixture.CreateDbContext();
        var system = await CreateSystemAsync(db);
        var repository = new RuleEntryRepository(db);
        var marker = $"Findme{Guid.NewGuid():N}";
        await repository.AddAsync(CreateEntry(system.Id, marker));
        await repository.AddAsync(CreateEntry(system.Id, "Something Else"));
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new RuleEntryRepository(readDb).SearchAsync(
            system.Id, RuleCategory.Class, marker.ToLowerInvariant(), 1, 10);

        Assert.Single(results);
        Assert.Equal(marker, results[0].Title);
    }

    [Fact]
    public async Task SearchAsync_DoesNotLeakEntriesFromAnotherCategory()
    {
        await using var db = fixture.CreateDbContext();
        var system = await CreateSystemAsync(db);
        var repository = new RuleEntryRepository(db);
        var marker = $"Category{Guid.NewGuid():N}";
        await repository.AddAsync(CreateEntry(system.Id, marker, RuleCategory.Class));
        await repository.AddAsync(CreateEntry(system.Id, marker, RuleCategory.Race));
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new RuleEntryRepository(readDb).SearchAsync(
            system.Id, RuleCategory.Class, marker, 1, 10);

        Assert.Single(results);
        Assert.Equal(RuleCategory.Class, results[0].Category);
    }

    [Fact]
    public async Task CountAsync_MatchesTotalIndependentOfPageSize()
    {
        await using var db = fixture.CreateDbContext();
        var system = await CreateSystemAsync(db);
        var repository = new RuleEntryRepository(db);
        var marker = $"Count{Guid.NewGuid():N}";
        for (var i = 0; i < 3; i++)
            await repository.AddAsync(CreateEntry(system.Id, $"{marker}-{i}"));
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var readRepo = new RuleEntryRepository(readDb);
        var total = await readRepo.CountAsync(system.Id, RuleCategory.Class, marker, CancellationToken.None);
        var page = await readRepo.SearchAsync(system.Id, RuleCategory.Class, marker, 1, 2, CancellationToken.None);

        Assert.Equal(3, total);
        Assert.Equal(2, page.Count);
    }

    [Fact]
    public async Task GetBySlugsAsync_EmptyCollection_ReturnsEmptyWithoutQuerying()
    {
        await using var db = fixture.CreateDbContext();
        var system = await CreateSystemAsync(db);

        var results = await new RuleEntryRepository(db).GetBySlugsAsync(system.Id, RuleCategory.Class, []);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetBySlugsAsync_ReturnsOnlyRequestedSlugs()
    {
        await using var db = fixture.CreateDbContext();
        var system = await CreateSystemAsync(db);
        var repository = new RuleEntryRepository(db);
        var wanted = CreateEntry(system.Id, "Wanted");
        var unwanted = CreateEntry(system.Id, "Unwanted");
        await repository.AddAsync(wanted);
        await repository.AddAsync(unwanted);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new RuleEntryRepository(readDb).GetBySlugsAsync(
            system.Id, RuleCategory.Class, [wanted.Slug]);

        Assert.Single(results);
        Assert.Equal(wanted.Slug, results[0].Slug);
    }

    [Fact]
    public async Task Remove_DeletesEntryFromDatabase()
    {
        await using var db = fixture.CreateDbContext();
        var system = await CreateSystemAsync(db);
        var repository = new RuleEntryRepository(db);
        var entry = CreateEntry(system.Id, "Doomed");
        await repository.AddAsync(entry);
        await db.SaveChangesAsync();

        await using var deleteDb = fixture.CreateDbContext();
        var deleteRepo = new RuleEntryRepository(deleteDb);
        var toDelete = await deleteRepo.GetBySlugAsync(system.Id, RuleCategory.Class, entry.Slug);
        deleteRepo.Remove(toDelete!);
        await deleteDb.SaveChangesAsync();

        await using var verifyDb = fixture.CreateDbContext();
        var verified = await new RuleEntryRepository(verifyDb).GetBySlugAsync(system.Id, RuleCategory.Class, entry.Slug);
        Assert.Null(verified);
    }
}
