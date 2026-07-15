using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Infrastructure.Tests;

[Collection("Postgres")]
public class CharacterRepositoryIntegrationTests(PostgresFixture fixture)
{
    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsCharacter()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CharacterRepository(db);
        var ownerId = UserId.New();
        var character = Character.Create(ownerId, "Grog", "Human", "Fighter", 3).Value!;

        await repository.AddAsync(character);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var fetched = await new CharacterRepository(readDb).GetByIdAsync(character.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Grog", fetched!.Name);
        Assert.Equal(3, fetched.Level);
    }

    [Fact]
    public async Task GetByOwnerAsync_IncludesCharactersWhereUserIsOwner()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CharacterRepository(db);
        var ownerId = UserId.New();
        await repository.AddAsync(Character.Create(ownerId, "Owned Character", "Human", "Fighter", 1).Value!);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new CharacterRepository(readDb).GetByOwnerAsync(ownerId);

        Assert.Single(results);
        Assert.Equal("Owned Character", results[0].Name);
    }

    [Fact]
    public async Task GetByOwnerAsync_IncludesCharactersWhereUserIsCoOwner()
    {
        // Regression guard for the Postgres array-contains translation: CoOwnerIds is a Guid[]
        // column, and `.Contains` here must translate to a real `= ANY(co_owner_ids)` query —
        // this can't be verified against the EF InMemory provider.
        await using var db = fixture.CreateDbContext();
        var repository = new CharacterRepository(db);
        var ownerId = UserId.New();
        var coOwnerId = UserId.New();
        var character = Character.Create(ownerId, "Shared Character", "Elf", "Wizard", 1).Value!;
        character.AddCoOwner(coOwnerId.Value);
        await repository.AddAsync(character);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new CharacterRepository(readDb).GetByOwnerAsync(coOwnerId);

        Assert.Single(results);
        Assert.Equal("Shared Character", results[0].Name);
    }

    [Fact]
    public async Task GetByOwnerAsync_ExcludesCharactersOwnedBySomeoneElse()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CharacterRepository(db);
        await repository.AddAsync(Character.Create(UserId.New(), "Someone Else's", "Human", "Fighter", 1).Value!);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var results = await new CharacterRepository(readDb).GetByOwnerAsync(UserId.New());

        Assert.Empty(results);
    }

    [Fact]
    public async Task Delete_RemovesCharacterFromDatabase()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CharacterRepository(db);
        var character = Character.Create(UserId.New(), "Doomed", "Human", "Fighter", 1).Value!;
        await repository.AddAsync(character);
        await db.SaveChangesAsync();

        await using var deleteDb = fixture.CreateDbContext();
        var deleteRepo = new CharacterRepository(deleteDb);
        var toDelete = await deleteRepo.GetByIdAsync(character.Id);
        deleteRepo.Delete(toDelete!);
        await deleteDb.SaveChangesAsync();

        await using var verifyDb = fixture.CreateDbContext();
        var verified = await new CharacterRepository(verifyDb).GetByIdAsync(character.Id);
        Assert.Null(verified);
    }

    [Fact]
    public async Task Update_PersistsSheetChanges()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new CharacterRepository(db);
        var character = Character.Create(UserId.New(), "Before", "Human", "Fighter", 1).Value!;
        await repository.AddAsync(character);
        await db.SaveChangesAsync();

        await using var updateDb = fixture.CreateDbContext();
        var updateRepo = new CharacterRepository(updateDb);
        var toUpdate = await updateRepo.GetByIdAsync(character.Id);
        toUpdate!.SetPublic(true);
        updateRepo.Update(toUpdate);
        await updateDb.SaveChangesAsync();

        await using var verifyDb = fixture.CreateDbContext();
        var verified = await new CharacterRepository(verifyDb).GetByIdAsync(character.Id);
        Assert.True(verified!.IsPublic);
    }
}
