using TTRPGHub.Entities;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Infrastructure.Tests;

[Collection("Postgres")]
public class UserRepositoryIntegrationTests(PostgresFixture fixture)
{
    private static User CreateUser(string? emailOverride = null) => User.Create(
        $"u{Guid.NewGuid():N}"[..20],
        ValueObjects.Email.Create(emailOverride ?? $"{Guid.NewGuid():N}@test.com").Value!,
        "hashed-password");

    [Fact]
    public async Task AddAsync_ThenGetByIdAsync_RoundTripsUser()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new UserRepository(db);
        var user = CreateUser();

        await repository.AddAsync(user);
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var fetched = await new UserRepository(readDb).GetByIdAsync(user.Id);
        Assert.NotNull(fetched);
        Assert.Equal(user.Username, fetched!.Username);
        Assert.Equal(user.Email.Value, fetched.Email.Value);
    }

    [Fact]
    public async Task GetByEmailAsync_IsCaseSensitiveExactMatch()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new UserRepository(db);
        var email = $"{Guid.NewGuid():N}@test.com";
        await repository.AddAsync(CreateUser(email));
        await db.SaveChangesAsync();

        await using var readDb = fixture.CreateDbContext();
        var found = await new UserRepository(readDb).GetByEmailAsync(email);
        var notFound = await new UserRepository(readDb).GetByEmailAsync(email.ToUpperInvariant());

        Assert.NotNull(found);
        Assert.Null(notFound);
    }

    [Fact]
    public async Task ExistsByEmailAsync_UnknownEmail_ReturnsFalse()
    {
        await using var db = fixture.CreateDbContext();
        var exists = await new UserRepository(db).ExistsByEmailAsync($"{Guid.NewGuid():N}@nowhere.test");

        Assert.False(exists);
    }

    [Fact]
    public async Task Update_PersistsRoleChangeAcrossContexts()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new UserRepository(db);
        var user = CreateUser();
        await repository.AddAsync(user);
        await db.SaveChangesAsync();

        await using var updateDb = fixture.CreateDbContext();
        var updateRepo = new UserRepository(updateDb);
        var toUpdate = await updateRepo.GetByIdAsync(user.Id);
        toUpdate!.SetRole(UserRole.Moderator);
        updateRepo.Update(toUpdate);
        await updateDb.SaveChangesAsync();

        await using var verifyDb = fixture.CreateDbContext();
        var verified = await new UserRepository(verifyDb).GetByIdAsync(user.Id);
        Assert.Equal(UserRole.Moderator, verified!.Role);
    }

    [Fact]
    public async Task SearchAsync_FiltersByUsernameCaseInsensitively()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new UserRepository(db);
        var uniqueMarker = Guid.NewGuid().ToString("N")[..8];
        var user = User.Create($"Findme-{uniqueMarker}", ValueObjects.Email.Create($"{Guid.NewGuid():N}@test.com").Value!, "hash");
        await repository.AddAsync(user);
        await db.SaveChangesAsync();

        await using var searchDb = fixture.CreateDbContext();
        var (items, total) = await new UserRepository(searchDb).SearchAsync($"findme-{uniqueMarker}", 1, 10);

        Assert.Equal(1, total);
        Assert.Contains(items, u => u.Id == user.Id);
    }

    [Fact]
    public async Task SearchAsync_RespectsPagination()
    {
        await using var db = fixture.CreateDbContext();
        var repository = new UserRepository(db);
        var groupMarker = Guid.NewGuid().ToString("N")[..8];
        for (var i = 0; i < 3; i++)
        {
            await repository.AddAsync(User.Create(
                $"page-{groupMarker}-{i}", ValueObjects.Email.Create($"{Guid.NewGuid():N}@test.com").Value!, "hash"));
        }
        await db.SaveChangesAsync();

        await using var searchDb = fixture.CreateDbContext();
        var (page1, total) = await new UserRepository(searchDb).SearchAsync($"page-{groupMarker}", 1, 2);
        var (page2, _) = await new UserRepository(searchDb).SearchAsync($"page-{groupMarker}", 2, 2);

        Assert.Equal(3, total);
        Assert.Equal(2, page1.Count);
        Assert.Single(page2);
    }
}
