using MediatR;
using TTRPGHub.Entities.Events;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Events.Commands.CancelEvent;
using TTRPGHub.Features.Events.Commands.CreateEvent;
using TTRPGHub.Features.Events.Commands.RegisterForEvent;
using TTRPGHub.Features.Events.Commands.UnregisterFromEvent;
using TTRPGHub.Features.Events.Queries.GetEventDetail;
using TTRPGHub.Features.Events.Queries.GetEvents;

namespace TTRPGHub.API.Endpoints.Events;

public static class EventEndpoints
{
    public static IEndpointRouteBuilder MapEvents(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/events").WithTags("Events");

        g.MapGet("/", async (int page, int pageSize, string? location, string? format, IMediator m, CancellationToken ct) =>
        {
            EventFormat? parsedFormat = Enum.TryParse<EventFormat>(format, ignoreCase: true, out var f) ? f : null;
            return (await m.Send(new GetEventsQuery(page, pageSize, location, parsedFormat), ct)).ToResponse();
        })
            .AllowAnonymous();

        g.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
            (await m.Send(new GetEventDetailQuery(id), ct)).ToResponse())
            .AllowAnonymous();

        g.MapPost("/", async (CreateEventRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CreateEventCommand(
                req.Title, req.Description, req.System, req.Format,
                req.Location, req.OnlineLink, req.StartsAt, req.MaxParticipants), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/events/{result.Value}", result.Value)
                : result.ToResponse();
        }).RequireAuthorization();

        g.MapPost("/{id:guid}/register", async (Guid id, IMediator m, CancellationToken ct) =>
            (await m.Send(new RegisterForEventCommand(id), ct)).ToResponse())
            .RequireAuthorization();

        g.MapDelete("/{id:guid}/register", async (Guid id, IMediator m, CancellationToken ct) =>
            (await m.Send(new UnregisterFromEventCommand(id), ct)).ToResponse())
            .RequireAuthorization();

        g.MapPatch("/{id:guid}/cancel", async (Guid id, IMediator m, CancellationToken ct) =>
            (await m.Send(new CancelEventCommand(id), ct)).ToResponse())
            .RequireAuthorization();

        return app;
    }
}

public sealed record CreateEventRequest(
    string Title, string? Description, string System, string Format,
    string? Location, string? OnlineLink, DateTime StartsAt, int MaxParticipants);
