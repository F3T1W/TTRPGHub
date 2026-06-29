using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.SessionNotes.Commands.CreateNote;
using TTRPGHub.Features.SessionNotes.Commands.DeleteNote;
using TTRPGHub.Features.SessionNotes.Commands.UpdateNote;
using TTRPGHub.Features.SessionNotes.Queries.GetNoteDetail;
using TTRPGHub.Features.SessionNotes.Queries.GetNotesByCampaign;

namespace TTRPGHub.Endpoints.SessionNotes;

public static class SessionNotesEndpoints
{
    public static void MapSessionNotes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notes").RequireAuthorization();

        group.MapGet("/campaign/{campaignId:guid}", async (Guid campaignId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetNotesByCampaignQuery(campaignId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetNoteDetailQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapPost("/", async (CreateNoteRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new CreateNoteCommand(req.CampaignId, req.Title, req.Content, req.SessionDate), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/notes/{result.Value!.NoteId}", result.Value)
                : result.ToResponse();
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateNoteRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateNoteCommand(id, req.Title, req.Content, req.SessionDate), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteNoteCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });
    }
}

public record CreateNoteRequest(Guid CampaignId, string Title, string Content, DateTime SessionDate);
public record UpdateNoteRequest(string Title, string Content, DateTime SessionDate);
