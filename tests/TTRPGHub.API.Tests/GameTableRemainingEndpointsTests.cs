using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class GameTableRemainingEndpointsTests(ApiFactory factory)
{
    private static async Task<Guid> CreateSessionAsync(HttpClient client)
    {
        var create = await client.PostAsJsonAsync("/api/sessions", new
        {
            Title = "Open table",
            Description = (string?)null,
            System = "pf2e",
            MaxPlayers = 4,
            ScheduledAt = DateTime.UtcNow.AddDays(1),
            Format = "Online",
            Location = (string?)null
        });
        var body = await create.Content.ReadFromJsonAsync<CreateSessionResponseDto>();
        return body!.SessionId;
    }

    private static async Task<Guid> CreateSceneAsync(HttpClient client, Guid sessionId, string name = "The Dungeon")
    {
        var create = await client.PostAsJsonAsync($"/api/table/{sessionId}/scenes", new { Name = name });
        var body = await create.Content.ReadFromJsonAsync<CreateSceneResponseDto>();
        return body!.Id;
    }

    private static async Task<(Guid SessionId, Guid SceneId)> CreateSessionWithActiveSceneAsync(HttpClient client)
    {
        var sessionId = await CreateSessionAsync(client);
        var sceneId = await CreateSceneAsync(client, sessionId);
        var activate = await client.PostAsync($"/api/table/{sessionId}/scenes/{sceneId}/activate", null);
        Assert.Equal(HttpStatusCode.NoContent, activate.StatusCode);
        return (sessionId, sceneId);
    }

    private static async Task<Guid> AddTokenAsync(HttpClient client, Guid sessionId)
    {
        var add = await client.PostAsJsonAsync($"/api/table/{sessionId}/tokens", new
        {
            Label = "Goblin",
            ImageUrl = (string?)null,
            Color = "#ff0000",
            X = 5.0,
            Y = 5.0,
            OwnerUserId = (Guid?)null
        });
        var body = await add.Content.ReadFromJsonAsync<TableTokenDto>();
        return body!.Id;
    }

    // --- Tokens ---

    [Fact]
    public async Task AddToken_ByOrganizer_ReturnsOk()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PostAsJsonAsync($"/api/table/{sessionId}/tokens", new
        {
            Label = "Goblin", ImageUrl = (string?)null, Color = "#ff0000",
            X = 1.0, Y = 1.0, OwnerUserId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task MoveToken_ByNonOrganizer_ReturnsUnauthorized()
    {
        var organizer = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(organizer);
        var tokenId = await AddTokenAsync(organizer, sessionId);

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var response = await stranger.PutAsJsonAsync($"/api/table/{sessionId}/tokens/{tokenId}/position", new { X = 3.0, Y = 3.0 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTokenStats_ByOrganizer_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);
        var tokenId = await AddTokenAsync(client, sessionId);

        var response = await client.PatchAsJsonAsync($"/api/table/{sessionId}/tokens/{tokenId}/stats", new
        {
            CurrentHp = 10, Width = (int?)null, Height = (int?)null, Rotation = (int?)null,
            SetInitiative = false, Initiative = (int?)null, HasDarkvision = (bool?)null,
            HasLowLightVision = (bool?)null, CurrentStamina = (int?)null, MaxStamina = (int?)null,
            AddCoOwnerId = (Guid?)null, RemoveCoOwnerId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetTokenVisibility_ByOrganizer_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);
        var tokenId = await AddTokenAsync(client, sessionId);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/tokens/{tokenId}/visibility", new { VisibleToUserIds = (List<Guid>?)null });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ApplyThenRemoveTokenCondition_ReturnsSuccess()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);
        var tokenId = await AddTokenAsync(client, sessionId);

        var apply = await client.PostAsJsonAsync($"/api/table/{sessionId}/tokens/{tokenId}/conditions", new { Slug = "prone", Name = "Prone", Value = (int?)null });
        Assert.Equal(HttpStatusCode.NoContent, apply.StatusCode);

        var remove = await client.DeleteAsync($"/api/table/{sessionId}/tokens/{tokenId}/conditions/prone");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);
    }

    [Fact]
    public async Task RemoveToken_ByOrganizer_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);
        var tokenId = await AddTokenAsync(client, sessionId);

        var response = await client.DeleteAsync($"/api/table/{sessionId}/tokens/{tokenId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- Journal ---

    [Fact]
    public async Task CreateJournalEntry_ThenGetJournal_ReturnsEntry()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var create = await client.PostAsJsonAsync($"/api/table/{sessionId}/journal", new
        {
            Title = "Chapter 1", ContentMarkdown = "The party arrives.", ParentId = (Guid?)null, CampaignId = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var entry = await create.Content.ReadFromJsonAsync<JournalEntryDto>();

        var list = await client.GetAsync($"/api/table/{sessionId}/journal");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);

        var publish = await client.PutAsJsonAsync($"/api/table/{sessionId}/journal/{entry!.Id}/published", new { Published = true });
        Assert.Equal(HttpStatusCode.NoContent, publish.StatusCode);

        var visibility = await client.PutAsJsonAsync($"/api/table/{sessionId}/journal/{entry.Id}/visibility", new { VisibleToUserIds = (List<Guid>?)null });
        Assert.Equal(HttpStatusCode.NoContent, visibility.StatusCode);

        var update = await client.PutAsJsonAsync($"/api/table/{sessionId}/journal/{entry.Id}", new
        {
            Title = "Chapter 1 - Revised", ContentMarkdown = "The party arrives at dusk.", ParentId = (Guid?)null, CampaignId = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var delete = await client.DeleteAsync($"/api/table/{sessionId}/journal/{entry.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task CreateJournalEntry_ByNonOrganizer_ReturnsUnauthorized()
    {
        var organizer = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(organizer);

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var response = await stranger.PostAsJsonAsync($"/api/table/{sessionId}/journal", new
        {
            Title = "Chapter 1", ContentMarkdown = "Intrusion", ParentId = (Guid?)null, CampaignId = (Guid?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Scene environment/grid/fog/walls/lights ---

    [Fact]
    public async Task SetGridCellSize_ValidValue_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/grid", new { Px = 50 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetGridCellSize_InvalidValue_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/grid", new { Px = 5 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SetFogSettings_ValidValue_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/fog", new { Enabled = true, VisionRadiusFeet = 60 });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetFogSettings_InvalidRadius_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/fog", new { Enabled = true, VisionRadiusFeet = 1 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SetEnvironment_ValidLighting_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/environment", new { TerrainTagsJson = (string?)null, AmbientLighting = "bright" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetEnvironment_InvalidLighting_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/environment", new { TerrainTagsJson = (string?)null, AmbientLighting = "invalid" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task SetWalls_ByOrganizer_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var (sessionId, _) = await CreateSessionWithActiveSceneAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/walls", new { WallsJson = "[]" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetLights_ByOrganizer_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var (sessionId, _) = await CreateSessionWithActiveSceneAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/lights", new { LightsJson = "[]" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- Variant rules / encounter table ---

    [Fact]
    public async Task SetVariantRules_ByOrganizer_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/variant-rules", new
        {
            ProficiencyWithoutLevel = true, AutomaticBonusProgression = false,
            FreeArchetype = true, GradualAbilityBoosts = false, StaminaVariant = false
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetEncounterTable_ThenRoll_ReturnsResult()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var set = await client.PutAsJsonAsync($"/api/table/{sessionId}/encounter-table", new
        {
            EncounterTableJson = """{"title":"Forest Encounters","entries":[{"min":1,"max":20,"label":"A wandering goblin","monsterId":null}]}"""
        });
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        var roll = await client.PostAsync($"/api/table/{sessionId}/encounter-table/roll", null);
        Assert.Equal(HttpStatusCode.OK, roll.StatusCode);
    }

    [Fact]
    public async Task RollEncounterTable_WithoutTableSet_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PostAsync($"/api/table/{sessionId}/encounter-table/roll", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // --- Combat ---

    [Fact]
    public async Task StartCombat_ThenAdvanceTurn_ThenEndCombat_ReturnsSuccess()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);
        var tokenId = await AddTokenAsync(client, sessionId);
        var setInitiative = await client.PatchAsJsonAsync($"/api/table/{sessionId}/tokens/{tokenId}/stats", new
        {
            CurrentHp = (int?)null, Width = (int?)null, Height = (int?)null, Rotation = (int?)null,
            SetInitiative = true, Initiative = 15, HasDarkvision = (bool?)null,
            HasLowLightVision = (bool?)null, CurrentStamina = (int?)null, MaxStamina = (int?)null,
            AddCoOwnerId = (Guid?)null, RemoveCoOwnerId = (Guid?)null
        });
        Assert.Equal(HttpStatusCode.NoContent, setInitiative.StatusCode);

        var start = await client.PostAsync($"/api/table/{sessionId}/combat/start", null);
        Assert.Equal(HttpStatusCode.NoContent, start.StatusCode);

        var next = await client.PostAsync($"/api/table/{sessionId}/combat/next", null);
        Assert.Equal(HttpStatusCode.NoContent, next.StatusCode);

        var previous = await client.PostAsync($"/api/table/{sessionId}/combat/previous", null);
        Assert.Equal(HttpStatusCode.NoContent, previous.StatusCode);

        var end = await client.PostAsync($"/api/table/{sessionId}/combat/end", null);
        Assert.Equal(HttpStatusCode.NoContent, end.StatusCode);
    }

    [Fact]
    public async Task StartCombat_ByNonOrganizer_ReturnsUnauthorized()
    {
        var organizer = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(organizer);

        var stranger = await factory.CreateClient().AuthenticateAsync();
        var response = await stranger.PostAsync($"/api/table/{sessionId}/combat/start", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // --- Scenes: rename/delete/activate ---

    [Fact]
    public async Task RenameScene_ByOrganizer_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);
        var sceneId = await CreateSceneAsync(client, sessionId);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/scenes/{sceneId}", new { Name = "Renamed Scene" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteScene_LastRemainingScene_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var defaultSceneId = db.Scenes.AsEnumerable().Single(s => s.SessionId.Value == sessionId).Id;

        var response = await client.DeleteAsync($"/api/table/{sessionId}/scenes/{defaultSceneId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task DeleteScene_WithSecondSceneRemaining_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);
        var firstScene = await CreateSceneAsync(client, sessionId, "First");
        await CreateSceneAsync(client, sessionId, "Second");

        var response = await client.DeleteAsync($"/api/table/{sessionId}/scenes/{firstScene}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // --- Showcase / audio ---

    [Fact]
    public async Task SetShowcaseImage_ByOrganizer_ReturnsNoContent()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.PutAsJsonAsync($"/api/table/{sessionId}/showcase", new { ImageUrl = "https://example.com/image.png" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task SetTrack_ThenPlayPauseSeekClear_ReturnsSuccess()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var setTrack = await client.PutAsJsonAsync($"/api/table/{sessionId}/audio/track", new { TrackUrl = "https://example.com/track.mp3", TrackTitle = "Tavern Theme" });
        Assert.Equal(HttpStatusCode.NoContent, setTrack.StatusCode);

        var play = await client.PostAsJsonAsync($"/api/table/{sessionId}/audio/play", new { PositionSeconds = 0.0 });
        Assert.Equal(HttpStatusCode.NoContent, play.StatusCode);

        var pause = await client.PostAsJsonAsync($"/api/table/{sessionId}/audio/pause", new { PositionSeconds = 5.0 });
        Assert.Equal(HttpStatusCode.NoContent, pause.StatusCode);

        var seek = await client.PostAsJsonAsync($"/api/table/{sessionId}/audio/seek", new { PositionSeconds = 10.0 });
        Assert.Equal(HttpStatusCode.NoContent, seek.StatusCode);

        var clear = await client.DeleteAsync($"/api/table/{sessionId}/audio");
        Assert.Equal(HttpStatusCode.NoContent, clear.StatusCode);
    }

    // --- Whisper / characters ---

    [Fact]
    public async Task SendWhisper_ByNonOrganizer_ReturnsUnauthorized()
    {
        var organizer = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(organizer);

        var stranger = factory.CreateClient();
        var strangerId = await stranger.AuthenticateWithIdAsync();
        var response = await stranger.PostAsJsonAsync($"/api/table/{sessionId}/whisper", new { RecipientUserId = strangerId, Content = "Psst" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionCharacters_ByOrganizer_ReturnsOk()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var sessionId = await CreateSessionAsync(client);

        var response = await client.GetAsync($"/api/table/{sessionId}/characters");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record CreateSessionResponseDto(Guid SessionId, string Title);
    private sealed record CreateSceneResponseDto(Guid Id, string Name);
    private sealed record TableTokenDto(Guid Id, string Label, string? ImageUrl, string Color,
        double X, double Y, int Width, int Height, int Rotation, Guid? OwnerId, bool CanMove,
        string CombatantType, Guid? CombatantId, int? CurrentHp, int? MaxHp, int? ArmorClass,
        List<object> Conditions, int? Initiative, bool HasDarkvision, bool HasLowLightVision,
        List<Guid>? VisibleToUserIds);
    private sealed record JournalEntryDto(Guid Id, string Title, string ContentMarkdown, bool IsPublished,
        Guid? ParentId, Guid? CampaignId, List<Guid>? VisibleToUserIds, DateTime CreatedAt, DateTime UpdatedAt);
}
