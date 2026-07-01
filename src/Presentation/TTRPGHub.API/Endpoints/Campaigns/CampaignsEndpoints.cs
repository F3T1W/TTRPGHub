using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Campaigns.Commands.AddParticipant;
using TTRPGHub.Features.Campaigns.Commands.ChangeCampaignStatus;
using TTRPGHub.Features.Campaigns.Commands.CreateCampaign;
using TTRPGHub.Features.Campaigns.Commands.RemoveParticipant;
using TTRPGHub.Features.Campaigns.Commands.UpdateCampaign;
using TTRPGHub.Features.Campaigns.Queries.GetCampaignDetail;
using TTRPGHub.Features.Campaigns.Commands.ImportCampaign;
using TTRPGHub.Features.Campaigns.Queries.GetAllCampaigns;
using TTRPGHub.Features.Campaigns.Queries.GetMyCampaigns;
using TTRPGHub.Entities;

namespace TTRPGHub.Endpoints.Campaigns;

public static class CampaignsEndpoints
{
    public static void MapCampaigns(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/campaigns").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetAllCampaignsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        }).AllowAnonymous();

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCampaignsQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCampaignDetailQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapPost("/", async (CreateCampaignRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateCampaignCommand(req.Title, req.Description, req.System), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/campaigns/{result.Value!.CampaignId}", result.Value)
                : result.ToResponse();
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateCampaignRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateCampaignCommand(id, req.Title, req.Description, req.System), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapPost("/{id:guid}/participants", async (Guid id, AddParticipantRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddParticipantCommand(id, req.UserId), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapDelete("/{id:guid}/participants/{userId:guid}", async (Guid id, Guid userId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RemoveParticipantCommand(id, userId), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapPatch("/{id:guid}/status", async (Guid id, ChangeStatusRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ChangeCampaignStatusCommand(id, req.Status), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        });

        group.MapPost("/import", async (ImportCampaignRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ImportCampaignCommand(req.Title, req.System, req.Description), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/campaigns/{result.Value!.CampaignId}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Импортировать кампанию из JSON");
    }
}

public record ImportCampaignRequest(string Title, string System, string? Description = null);

public record CreateCampaignRequest(string Title, string? Description, string System);
public record UpdateCampaignRequest(string Title, string? Description, string System);
public record AddParticipantRequest(Guid UserId);
public record ChangeStatusRequest(CampaignStatus Status);
