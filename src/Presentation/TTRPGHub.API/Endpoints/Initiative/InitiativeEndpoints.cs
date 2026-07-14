using MediatR;
using TTRPGHub.Entities;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Initiative.Commands.CreateTracker;
using TTRPGHub.Features.Initiative.Commands.DeleteTracker;
using TTRPGHub.Features.Initiative.Commands.NextTurn;
using TTRPGHub.Features.Initiative.Commands.PreviousTurn;
using TTRPGHub.Features.Initiative.Commands.SetEntries;
using TTRPGHub.Features.Initiative.Commands.StartTracker;
using TTRPGHub.Features.Initiative.Commands.UpdateEntry;
using TTRPGHub.Features.Initiative.Commands.LinkTrackerSession;
using TTRPGHub.Features.Initiative.Commands.SyncTrackerFromTable;
using TTRPGHub.Features.Initiative.Queries.GetTrackerDetail;
using TTRPGHub.Features.Initiative.Queries.GetTrackersByCampaign;

namespace TTRPGHub.Endpoints.Initiative;

public static class InitiativeEndpoints
{
    public static void MapInitiative(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/trackers").RequireAuthorization();

        group.MapGet("/campaign/{campaignId:guid}", async (Guid campaignId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTrackersByCampaignQuery(campaignId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetTrackerDetailQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapPost("/", async (CreateTrackerRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateTrackerCommand(req.CampaignId, req.Name), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/trackers/{result.Value!.TrackerId}", result.Value)
                : result.ToResponse();
        });

        group.MapPost("/{id:guid}/entries", async (Guid id, List<EntryInputDto> entries, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetEntriesCommand(id,
                entries.Select(e => new EntryInput(e.Name, e.Initiative, e.MaxHp, e.CurrentHp,
                    e.ArmorClass, e.IsPlayerCharacter, e.Notes)).ToList()), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapPatch("/{id:guid}/entries/{entryId:guid}", async (Guid id, Guid entryId, UpdateEntryDto req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateEntryCommand(id, entryId, req.CurrentHp, req.Status, req.Notes), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapPost("/{id:guid}/start", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new StartTrackerCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapPost("/{id:guid}/next", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new NextTurnCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapPost("/{id:guid}/previous", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new PreviousTurnCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapPost("/{id:guid}/sync-from-table", async (Guid id, SyncFromTableRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SyncTrackerFromTableCommand(id, req.SessionId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapPatch("/{id:guid}/link-session", async (Guid id, LinkSessionRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LinkTrackerSessionCommand(id, req.SessionId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteTrackerCommand(id), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });
    }
}

public record CreateTrackerRequest(Guid CampaignId, string Name);
public record EntryInputDto(string Name, int Initiative, int MaxHp, int CurrentHp,
    int ArmorClass, bool IsPlayerCharacter, string? Notes);
public record UpdateEntryDto(int CurrentHp, EntryStatus Status, string? Notes);
public record SyncFromTableRequest(Guid SessionId);
public record LinkSessionRequest(Guid? SessionId);
