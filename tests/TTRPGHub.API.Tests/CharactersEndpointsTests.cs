using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class CharactersEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Create_ThenGetMine_ReturnsCreatedCharacter()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/characters", new
        {
            Name = "Grog",
            Race = "Human",
            Class = "Fighter",
            Level = 1
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var mine = await client.GetAsync("/api/characters/me");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var list = await mine.Content.ReadFromJsonAsync<List<CharacterSummaryDto>>();
        Assert.Contains(list!, c => c.Name == "Grog");
    }

    [Fact]
    public async Task Create_InvalidLevel_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/characters", new
        {
            Name = "Grog",
            Race = "Human",
            Class = "Fighter",
            Level = 0
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    [Fact]
    public async Task GetDetail_NonExistentId_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync($"/api/characters/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetDetail_OtherUsersPrivateCharacter_ReturnsUnauthorized()
    {
        var owner = await factory.CreateClient().AuthenticateAsync();
        var create = await owner.PostAsJsonAsync("/api/characters", new { Name = "Owned", Race = "Elf", Class = "Wizard", Level = 1 });
        var created = await create.Content.ReadFromJsonAsync<CreateCharacterResponseDto>();

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var response = await stranger.GetAsync($"/api/characters/{created!.CharacterId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record CharacterSummaryDto(Guid Id, string Name, string Race, string Class, int Level, string? AvatarUrl, DateTime UpdatedAt);
    private sealed record CreateCharacterResponseDto(Guid CharacterId, string Name, string Race, string Class, int Level);
}
