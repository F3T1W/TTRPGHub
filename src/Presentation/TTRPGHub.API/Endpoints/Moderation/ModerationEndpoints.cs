using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Moderation;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Moderation.Commands.CreateReport;
using TTRPGHub.Features.Moderation.Commands.ResolveReport;
using TTRPGHub.Features.Moderation.Queries.GetModerationLog;
using TTRPGHub.Features.Moderation.Queries.GetOpenReports;

namespace TTRPGHub.Endpoints.Moderation;

internal static class ModerationEndpoints
{
    internal static void MapModerationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/reports").WithTags("Moderation").RequireAuthorization();

        group.MapPost("/", async (CreateReportRequest req, ISender sender, CancellationToken ct) =>
        {
            if (!Enum.TryParse<ReportedEntityType>(req.EntityType, ignoreCase: true, out var entityType))
                return Result<Guid>.Failure(Error.Validation("EntityType", "Неизвестный тип контента.")).ToResponse();

            var result = await sender.Send(new CreateReportCommand(entityType, req.EntityId, req.Reason), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/reports/{result.Value}", result.Value)
                : result.ToResponse();
        });

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetOpenReportsQuery(), ct)).ToResponse())
            .RequireAuthorization(p => p.RequireRole("Moderator", "Admin"));

        group.MapPatch("/{id:guid}/resolve", async (Guid id, ResolveReportRequest req, ISender sender, CancellationToken ct) =>
                (await sender.Send(new ResolveReportCommand(id, req.Status), ct)).ToResponse())
            .RequireAuthorization(p => p.RequireRole("Moderator", "Admin"));

        app.MapGet("/api/v1/moderation-log", async (ISender sender, CancellationToken ct) =>
                (await sender.Send(new GetModerationLogQuery(), ct)).ToResponse())
            .WithTags("Moderation")
            .RequireAuthorization(p => p.RequireRole("Moderator", "Admin"));
    }
}

internal sealed record CreateReportRequest(string EntityType, Guid EntityId, string Reason);
internal sealed record ResolveReportRequest(string Status);
