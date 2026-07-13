using MediatR;
using TTRPGHub.Entities;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Ratings.Commands.RateSessionParticipant;
using TTRPGHub.Features.Ratings.Queries.GetSessionReviews;
using TTRPGHub.Features.Sessions.Commands.ChangeSessionStatus;
using TTRPGHub.Features.Sessions.Commands.CreateSession;
using TTRPGHub.Features.Sessions.Commands.ImportSession;
using TTRPGHub.Features.Sessions.Commands.JoinSession;
using TTRPGHub.Features.Sessions.Commands.LeaveSession;
using TTRPGHub.Features.Sessions.Commands.UpdateSession;
using TTRPGHub.Features.Sessions.Queries.GetMySessions;
using TTRPGHub.Features.Sessions.Queries.GetSessionDetail;
using TTRPGHub.Features.Sessions.Queries.GetUpcomingSessions;

namespace TTRPGHub.Endpoints.Sessions;

internal static class SessionsEndpoints
{
    internal static void MapSessionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sessions")
            .WithTags("Sessions")
            .RequireAuthorization();

        group.MapGet("/upcoming", async ([AsParameters] GetUpcomingSessionsQuery query, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(query, ct);
            return result.ToResponse();
        })
        .WithSummary("Ближайшие открытые сессии")
        .Produces<IReadOnlyList<SessionSummaryDto>>(StatusCodes.Status200OK);

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMySessionsQuery(), ct);
            return result.ToResponse();
        })
        .WithSummary("Мои сессии")
        .Produces<IReadOnlyList<SessionSummaryDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetSessionDetailQuery(id), ct);
            return result.ToResponse();
        })
        .WithSummary("Детали сессии")
        .Produces<SessionDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", async (CreateSessionCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/sessions/{result.Value!.SessionId}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Создать сессию")
        .Produces<CreateSessionResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}", async (Guid id, UpdateSessionCommand command, ISender sender, CancellationToken ct) =>
        {
            if (id != command.SessionId)
                return Results.BadRequest("ID в URL не совпадает с телом запроса.");
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Обновить сессию")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/join", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new JoinSessionCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Вступить в сессию")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/leave", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LeaveSessionCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Покинуть сессию")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPatch("/{id:guid}/status", async (Guid id, ChangeStatusRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ChangeSessionStatusCommand(id, req.Status), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Изменить статус сессии")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/import", async (ImportSessionCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/sessions/{result.Value!.SessionId}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Импортировать сессию из JSON");

        group.MapGet("/{id:guid}/reviews", async (Guid id, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetSessionReviewsQuery(id), ct)).ToResponse())
            .WithSummary("Отзывы участников по конкретной сессии");

        group.MapPost("/{id:guid}/reviews", async (Guid id, RateSessionParticipantRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RateSessionParticipantCommand(id, req.RevieweeId, req.Score, req.Comment), ct);
            return result.IsSuccess
                ? Results.Created($"/api/sessions/{id}/reviews/{result.Value}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Оставить отзыв участнику сыгранной сессии");
    }
}

internal sealed record ChangeStatusRequest(SessionStatus Status);
internal sealed record RateSessionParticipantRequest(Guid RevieweeId, int Score, string? Comment);
