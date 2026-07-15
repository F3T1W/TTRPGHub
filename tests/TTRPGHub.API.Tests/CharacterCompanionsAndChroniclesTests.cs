using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class CharacterCompanionsAndChroniclesTests(ApiFactory factory)
{
    private static async Task<Guid> CreateCharacterAsync(HttpClient client)
    {
        var create = await client.PostAsJsonAsync("/api/characters", new
        {
            Name = "Grog",
            Race = "Human",
            Class = "Fighter",
            Level = 5
        });
        var body = await create.Content.ReadFromJsonAsync<CreateCharacterResponseDto>();
        return body!.CharacterId;
    }

    [Fact]
    public async Task CreateChronicle_ThenGetChronicles_ReturnsChronicle()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var characterId = await CreateCharacterAsync(client);

        var create = await client.PostAsJsonAsync($"/api/characters/{characterId}/chronicles", new
        {
            ScenarioName = "Scenario 1-01",
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            GmName = "GM Dave",
            Faction = "Grand Lodge",
            GoldEarned = 12,
            AchievementPoints = 4,
            BoonsUsed = (string?)null,
            Notes = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var list = await client.GetAsync($"/api/characters/{characterId}/chronicles");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task CreateChronicle_ByNonOwner_ReturnsUnauthorized()
    {
        var owner = await factory.CreateClient().AuthenticateAsync();
        var characterId = await CreateCharacterAsync(owner);

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var create = await stranger.PostAsJsonAsync($"/api/characters/{characterId}/chronicles", new
        {
            ScenarioName = "Scenario 1-01",
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            GmName = (string?)null,
            Faction = (string?)null,
            GoldEarned = 0,
            AchievementPoints = 0,
            BoonsUsed = (string?)null,
            Notes = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task CreateChronicle_EmptyScenarioName_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var characterId = await CreateCharacterAsync(client);

        var create = await client.PostAsJsonAsync($"/api/characters/{characterId}/chronicles", new
        {
            ScenarioName = "",
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            GmName = (string?)null,
            Faction = (string?)null,
            GoldEarned = 0,
            AchievementPoints = 0,
            BoonsUsed = (string?)null,
            Notes = (string?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    [Fact]
    public async Task DeleteChronicle_ThenGetChronicles_NoLongerListed()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var characterId = await CreateCharacterAsync(client);
        var create = await client.PostAsJsonAsync($"/api/characters/{characterId}/chronicles", new
        {
            ScenarioName = "Scenario 1-01",
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            GmName = (string?)null,
            Faction = (string?)null,
            GoldEarned = 0,
            AchievementPoints = 0,
            BoonsUsed = (string?)null,
            Notes = (string?)null
        });
        var created = await create.Content.ReadFromJsonAsync<CreateChronicleResponseDto>();

        var delete = await client.DeleteAsync($"/api/characters/{characterId}/chronicles/{created!.ChronicleId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var list = await client.GetAsync($"/api/characters/{characterId}/chronicles");
        var chronicles = await list.Content.ReadFromJsonAsync<List<ChronicleDto>>();
        Assert.DoesNotContain(chronicles!, c => c.Id == created.ChronicleId);
    }

    [Fact]
    public async Task CreateCompanion_ThenGetCompanions_ReturnsCompanion()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var characterId = await CreateCharacterAsync(client);

        var create = await client.PostAsJsonAsync($"/api/characters/{characterId}/companions", new
        {
            Name = "Fang",
            Kind = "Wolf Animal Companion",
            Level = 5,
            MaxHitPoints = 40,
            ArmorClass = 20,
            Speed = "40 feet",
            AttacksText = (string?)null,
            AbilitiesText = (string?)null,
            Notes = (string?)null
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<CreateCompanionResponseDto>();

        var list = await client.GetAsync($"/api/characters/{characterId}/companions");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var byId = await client.GetAsync($"/api/companions/{created!.CompanionId}");
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
    }

    [Fact]
    public async Task CreateCompanion_InvalidLevel_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var characterId = await CreateCharacterAsync(client);

        var create = await client.PostAsJsonAsync($"/api/characters/{characterId}/companions", new
        {
            Name = "Fang",
            Kind = "Wolf",
            Level = 21,
            MaxHitPoints = 40,
            ArmorClass = (int?)null,
            Speed = (string?)null,
            AttacksText = (string?)null,
            AbilitiesText = (string?)null,
            Notes = (string?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    [Fact]
    public async Task UpdateCompanion_ThenDelete_RemovesCompanion()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var characterId = await CreateCharacterAsync(client);
        var create = await client.PostAsJsonAsync($"/api/characters/{characterId}/companions", new
        {
            Name = "Fang",
            Kind = "Wolf",
            Level = 5,
            MaxHitPoints = 40,
            ArmorClass = (int?)null,
            Speed = (string?)null,
            AttacksText = (string?)null,
            AbilitiesText = (string?)null,
            Notes = (string?)null
        });
        var created = await create.Content.ReadFromJsonAsync<CreateCompanionResponseDto>();

        var update = await client.PutAsJsonAsync($"/api/characters/{characterId}/companions/{created!.CompanionId}", new
        {
            Name = "Fang the Fierce",
            Kind = "Wolf",
            Level = 5,
            MaxHitPoints = 40,
            CurrentHitPoints = 30,
            ArmorClass = (int?)null,
            Speed = (string?)null,
            AttacksText = (string?)null,
            AbilitiesText = (string?)null,
            Notes = (string?)null
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var delete = await client.DeleteAsync($"/api/characters/{characterId}/companions/{created.CompanionId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    private sealed record CreateCharacterResponseDto(Guid CharacterId, string Name, string Race, string Class, int Level);
    private sealed record CreateChronicleResponseDto(Guid ChronicleId);
    private sealed record CreateCompanionResponseDto(Guid CompanionId);
    private sealed record ChronicleDto(Guid Id, string ScenarioName, DateOnly SessionDate, string? GmName,
        string? Faction, int GoldEarned, int AchievementPoints, string? BoonsUsed, string? Notes);
}
