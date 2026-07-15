using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class ModulesEndpointsTests(ApiFactory factory)
{
    private static async Task<Guid> CreateMacroAsync(HttpClient client)
    {
        var create = await client.PostAsJsonAsync("/api/v1/macros", new
        {
            Name = "Fireball",
            ImageUrl = (string?)null,
            Type = "Chat",
            Command = "/roll 8d6"
        });
        var body = await create.Content.ReadFromJsonAsync<MacroDto>();
        return body!.Id;
    }

    [Fact]
    public async Task Export_WithMacro_ReturnsManifestJson()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var macroId = await CreateMacroAsync(client);

        var response = await client.PostAsJsonAsync("/api/v1/modules/export", new
        {
            Name = "My Module",
            Description = "A test module",
            Version = "1.0.0",
            MacroIds = new[] { macroId },
            SystemSlug = (string?)null
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<string>();
        Assert.False(string.IsNullOrWhiteSpace(json));
    }

    [Fact]
    public async Task Export_EmptyModule_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.PostAsJsonAsync("/api/v1/modules/export", new
        {
            Name = "My Module",
            Description = "A test module",
            Version = "1.0.0",
            MacroIds = Array.Empty<Guid>(),
            SystemSlug = (string?)null
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Export_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/modules/export", new
        {
            Name = "My Module",
            Description = (string?)null,
            Version = (string?)null,
            MacroIds = Array.Empty<Guid>(),
            SystemSlug = (string?)null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Export_NonExistentSystemSlug_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.PostAsJsonAsync("/api/v1/modules/export", new
        {
            Name = "My Module",
            Description = (string?)null,
            Version = (string?)null,
            MacroIds = Array.Empty<Guid>(),
            SystemSlug = $"nonexistent-{Guid.NewGuid():N}"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Export_ThenImport_RoundTripsSuccessfully()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var macroId = await CreateMacroAsync(client);

        var export = await client.PostAsJsonAsync("/api/v1/modules/export", new
        {
            Name = "Round Trip Module",
            Description = (string?)null,
            Version = "1.0.0",
            MacroIds = new[] { macroId },
            SystemSlug = (string?)null
        });
        var manifestJson = await export.Content.ReadFromJsonAsync<string>();

        using var form = new MultipartFormDataContent();
        var fileContent = new StringContent(manifestJson!);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        form.Add(fileContent, "file", "module.json");

        var import = await client.PostAsync("/api/v1/modules/import", form);

        Assert.Equal(HttpStatusCode.OK, import.StatusCode);
    }

    [Fact]
    public async Task Import_InvalidJson_ReturnsUnprocessableEntity()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        using var form = new MultipartFormDataContent();
        var fileContent = new StringContent("not valid json {{{");
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        form.Add(fileContent, "file", "module.json");

        var response = await client.PostAsync("/api/v1/modules/import", form);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    private sealed record MacroDto(Guid Id, string Name, string? ImageUrl, string Type, string Command,
        int HotbarSlot, DateTime CreatedAt, DateTime UpdatedAt);
}
