using System.Net;
using System.Net.Http.Json;

namespace TTRPGHub.API.Tests;

[Collection("Api")]
public class CalendarEndpointsTests(ApiFactory factory)
{
    [Fact]
    public async Task UpsertPreferences_ThenGet_ReturnsSavedValue()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var upsert = await client.PostAsJsonAsync("/api/calendar/preferences", new { ReminderMinutes = 30, RegenerateToken = false });
        Assert.Equal(HttpStatusCode.OK, upsert.StatusCode);
        var saved = await upsert.Content.ReadFromJsonAsync<CalendarPreferenceDto>();
        Assert.Equal(30, saved!.ReminderMinutes);

        var get = await client.GetAsync("/api/calendar/preferences");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var fetched = await get.Content.ReadFromJsonAsync<CalendarPreferenceDto>();
        Assert.Equal(30, fetched!.ReminderMinutes);
    }

    [Fact]
    public async Task GetPreferences_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/calendar/preferences");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionIcs_NonExistentSession_ReturnsNotFound()
    {
        var client = await factory.CreateClient().AuthenticateAsync();

        var response = await client.GetAsync($"/api/calendar/sessions/{Guid.NewGuid()}.ics");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_InvalidToken_ReturnsNotFound()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/calendar/feed/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFeed_AfterUpsertingPreferences_ReturnsIcsFeed()
    {
        var client = await factory.CreateClient().AuthenticateAsync();
        var upsert = await client.PostAsJsonAsync("/api/calendar/preferences", new { ReminderMinutes = 60, RegenerateToken = false });
        var saved = await upsert.Content.ReadFromJsonAsync<CalendarPreferenceDto>();

        var anonymous = factory.CreateClient();
        var feed = await anonymous.GetAsync($"/api/calendar/feed/{saved!.CalendarToken}");

        Assert.Equal(HttpStatusCode.OK, feed.StatusCode);
    }

    [Fact]
    public async Task GetVapidPublicKey_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/calendar/push/vapid-public-key");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record CalendarPreferenceDto(Guid CalendarToken, int ReminderMinutes, bool PushEnabled);
}
