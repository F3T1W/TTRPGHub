using MediatR;
using TTRPGHub.Entities;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Encounters.Commands.CreateEncounter;
using TTRPGHub.Features.Encounters.Commands.DeleteEncounter;
using TTRPGHub.Features.Encounters.Commands.UpdateEncounter;
using TTRPGHub.Features.Encounters.Queries.GetEncounterDetail;
using TTRPGHub.Features.Encounters.Queries.GetEncountersByCampaign;

namespace TTRPGHub.Endpoints.Encounters;

public static class EncountersEndpoints
{
    public static void MapEncounters(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/encounters").RequireAuthorization();

        group.MapGet("/campaign/{campaignId:guid}", async (Guid campaignId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEncountersByCampaignQuery(campaignId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetEncounterDetailQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapPost("/", async (CreateEncounterRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateEncounterCommand(
                req.CampaignId, req.Title, req.Description, req.Difficulty, req.Notes,
                req.Entries.Select(e => new EncounterEntryInput(e.Name, e.Count, e.Notes)).ToList()), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/encounters/{result.Value!.EncounterId}", result.Value)
                : result.ToResponse();
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateEncounterRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateEncounterCommand(
                id, req.Title, req.Description, req.Difficulty, req.Notes,
                req.Entries.Select(e => new EncounterEntryInput(e.Name, e.Count, e.Notes)).ToList()), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteEncounterCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });
    }
}

public record EncounterEntryDto(string Name, int Count, string? Notes);
public record CreateEncounterRequest(
    Guid CampaignId, string Title, string? Description,
    EncounterDifficulty Difficulty, string? Notes,
    List<EncounterEntryDto> Entries);
public record UpdateEncounterRequest(
    string Title, string? Description,
    EncounterDifficulty Difficulty, string? Notes,
    List<EncounterEntryDto> Entries);
