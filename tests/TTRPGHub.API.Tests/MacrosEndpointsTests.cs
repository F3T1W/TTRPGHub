using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class MacrosEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task Create_ThenGetMine_ReturnsMacro()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/macros", new
        {
            Name = "Fireball",
            ImageUrl = (string?)null,
            Type = "Chat",
            Command = "/roll 8d6"
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = await create.Content.ReadFromJsonAsync<MacroDto>();
        Assert.Equal("Fireball", created!.Name);

        var mine = await client.GetAsync("/api/v1/macros");
        Assert.Equal(HttpStatusCode.OK, mine.StatusCode);
        var list = await mine.Content.ReadFromJsonAsync<List<MacroDto>>();
        Assert.Contains(list!, m => m.Id == created.Id);
    }

    [Fact]
    public async Task Create_InvalidType_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var create = await client.PostAsJsonAsync("/api/v1/macros", new
        {
            Name = "Fireball",
            ImageUrl = (string?)null,
            Type = "NotARealType",
            Command = "/roll 8d6"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, create.StatusCode);
    }

    [Fact]
    public async Task Create_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var create = await client.PostAsJsonAsync("/api/v1/macros", new
        {
            Name = "Fireball",
            ImageUrl = (string?)null,
            Type = "Chat",
            Command = "/roll 8d6"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, create.StatusCode);
    }

    [Fact]
    public async Task Update_ThenDelete_RemovesMacro()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var create = await client.PostAsJsonAsync("/api/v1/macros", new
        {
            Name = "Fireball",
            ImageUrl = (string?)null,
            Type = "Chat",
            Command = "/roll 8d6"
        });
        var created = await create.Content.ReadFromJsonAsync<MacroDto>();

        var update = await client.PutAsJsonAsync($"/api/v1/macros/{created!.Id}", new
        {
            Name = "Fireball+",
            ImageUrl = (string?)null,
            Type = "Chat",
            Command = "/roll 10d6"
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var delete = await client.DeleteAsync($"/api/v1/macros/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var mine = await client.GetAsync("/api/v1/macros");
        var list = await mine.Content.ReadFromJsonAsync<List<MacroDto>>();
        Assert.DoesNotContain(list!, m => m.Id == created.Id);
    }

    [Fact]
    public async Task SetHotbarSlot_UpdatesSlot()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var create = await client.PostAsJsonAsync("/api/v1/macros", new
        {
            Name = "Fireball",
            ImageUrl = (string?)null,
            Type = "Chat",
            Command = "/roll 8d6"
        });
        var created = await create.Content.ReadFromJsonAsync<MacroDto>();

        var response = await client.PutAsJsonAsync($"/api/v1/macros/{created!.Id}/hotbar-slot", new { Slot = 3 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    private sealed record MacroDto(Guid Id, string Name, string? ImageUrl, string Type, string Command,
        int HotbarSlot, DateTime CreatedAt, DateTime UpdatedAt);
}
