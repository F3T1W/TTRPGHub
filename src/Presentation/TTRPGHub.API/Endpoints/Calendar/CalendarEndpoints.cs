using MediatR;
using Microsoft.Extensions.Configuration;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Calendar.Commands.SubscribePush;
using TTRPGHub.Features.Calendar.Commands.UnsubscribePush;
using TTRPGHub.Features.Calendar.Commands.UpsertCalendarPreference;
using TTRPGHub.Features.Calendar.Queries.GetCalendarFeed;
using TTRPGHub.Features.Calendar.Queries.GetCalendarPreference;
using TTRPGHub.Features.Calendar.Queries.GetSessionIcs;

namespace TTRPGHub.Endpoints.Calendar;

internal static class CalendarEndpoints
{
    internal static void MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/calendar").WithTags("Calendar");

        // Authenticated: save reminder preference and get webcal token
        group.MapPost("/preferences", async (UpsertCalendarPreferenceCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToResponse();
        })
        .RequireAuthorization()
        .WithSummary("Сохранить настройки напоминания")
        .Produces<CalendarPreferenceDto>(StatusCodes.Status200OK);

        // Authenticated: get current preferences
        group.MapGet("/preferences", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCalendarPreferenceQuery(), ct);
            return result.ToResponse();
        })
        .RequireAuthorization()
        .WithSummary("Текущие настройки напоминания")
        .Produces<CalendarPreferenceDto>(StatusCodes.Status200OK);

        // Authenticated: download single session .ics
        group.MapGet("/sessions/{id:guid}.ics", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSessionIcsQuery(id, 0), ct);
            if (!result.IsSuccess) return result.ToResponse();
            return Results.Text(result.Value!, "text/calendar; charset=utf-8");
        })
        .RequireAuthorization()
        .WithSummary("Скачать .ics для сессии")
        .Produces<string>(StatusCodes.Status200OK, "text/calendar");

        // Public (token-auth): iCal feed for calendar apps (no JWT — calendar clients don't send auth headers)
        app.MapGet("/api/calendar/feed/{token:guid}", async (Guid token, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCalendarFeedQuery(token), ct);
            if (!result.IsSuccess) return Results.NotFound();
            return Results.Text(result.Value!, "text/calendar; charset=utf-8");
        })
        .WithTags("Calendar")
        .AllowAnonymous()
        .WithSummary("Персональный iCal-фид (webcal)")
        .Produces<string>(StatusCodes.Status200OK, "text/calendar");

        // Public: VAPID public key, needed by frontend to register a push subscription
        group.MapGet("/push/vapid-public-key", (IConfiguration configuration) =>
            Results.Text(configuration["Vapid:PublicKey"] ?? string.Empty))
        .AllowAnonymous()
        .WithSummary("Публичный VAPID-ключ для push-подписки");

        group.MapPost("/push/subscribe", async (SubscribePushRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SubscribePushCommand(request.Endpoint, request.P256dh, request.Auth), ct);
            return result.ToResponse();
        })
        .RequireAuthorization()
        .WithSummary("Подписаться на push-уведомления");

        group.MapPost("/push/unsubscribe", async (UnsubscribePushRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UnsubscribePushCommand(request.Endpoint), ct);
            return result.ToResponse();
        })
        .RequireAuthorization()
        .WithSummary("Отписаться от push-уведомлений");
    }
}

internal sealed record SubscribePushRequest(string Endpoint, string P256dh, string Auth);
internal sealed record UnsubscribePushRequest(string Endpoint);
