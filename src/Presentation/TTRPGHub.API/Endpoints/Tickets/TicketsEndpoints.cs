using MediatR;
using Microsoft.AspNetCore.Mvc;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Tickets.Commands.ChangeTicketStatus;
using TTRPGHub.Features.Tickets.Commands.CreateTicket;
using TTRPGHub.Features.Tickets.Queries.GetAllTickets;
using TTRPGHub.Features.Tickets.Queries.GetMyTickets;

namespace TTRPGHub.Endpoints.Tickets;

public static class TicketsEndpoints
{
    public static void MapTickets(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tickets").RequireAuthorization();

        group.MapPost("/", async (
                [FromForm] string title,
                [FromForm] string description,
                [FromForm] string? contactInfo,
                IFormFileCollection? files,
                ISender sender, CancellationToken ct) =>
            {
                var uploads = (files ?? Enumerable.Empty<IFormFile>())
                    .Select(f => new TicketFileUpload(f.OpenReadStream(), f.FileName, f.ContentType, f.Length))
                    .ToList();

                var result = await sender.Send(new CreateTicketCommand(title, description, contactInfo, uploads), ct);
                return result.IsSuccess
                    ? Results.Created($"/api/tickets/{result.Value}", new { id = result.Value })
                    : result.ToResponse();
            })
            .DisableAntiforgery()
            .WithSummary("Создать тикет поддержки с вложениями");

        group.MapGet("/me", async (int page, int pageSize, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetMyTicketsQuery(page, pageSize), ct)).ToResponse());

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetAllTicketsQuery(), ct)).ToResponse())
            .RequireAuthorization(p => p.RequireRole("Moderator", "Admin"))
            .WithSummary("Все тикеты — данные для канбан-доски модератора");

        group.MapPatch("/{id:guid}/status", async (Guid id, ChangeTicketStatusRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ChangeTicketStatusCommand(id, req.Status), ct)).ToResponse())
            .RequireAuthorization(p => p.RequireRole("Moderator", "Admin"));
    }
}

public sealed record ChangeTicketStatusRequest(string Status);
