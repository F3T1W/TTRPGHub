using MediatR;
using TTRPGHub.Entities.Homebrew;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Homebrew.Commands.CreateHomebrew;
using TTRPGHub.Features.Homebrew.Commands.DeleteHomebrew;
using TTRPGHub.Features.Homebrew.Commands.ToggleHomebrewLike;
using TTRPGHub.Features.Homebrew.Queries.GetHomebrewDetail;
using TTRPGHub.Features.Homebrew.Queries.SearchHomebrew;

namespace TTRPGHub.API.Endpoints.Homebrew;

public static class HomebrewEndpoints
{
    public static IEndpointRouteBuilder MapHomebrew(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/homebrew").WithTags("Homebrew");

        g.MapGet("/", async (
            string? query, string? system, HomebrewType? type, string? tag,
            int page, int pageSize, IMediator m, CancellationToken ct) =>
            (await m.Send(new SearchHomebrewQuery(query, system, type, tag, page, pageSize), ct)).ToResponse())
            .AllowAnonymous();

        g.MapGet("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
            (await m.Send(new GetHomebrewDetailQuery(id), ct)).ToResponse())
            .AllowAnonymous();

        g.MapPost("/", async (CreateHomebrewCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/homebrew/{result.Value}", result.Value)
                : result.ToResponse();
        }).RequireAuthorization();

        g.MapDelete("/{id:guid}", async (Guid id, IMediator m, CancellationToken ct) =>
            (await m.Send(new DeleteHomebrewCommand(id), ct)).ToResponse())
            .RequireAuthorization();

        g.MapPost("/{id:guid}/like", async (Guid id, IMediator m, CancellationToken ct) =>
            (await m.Send(new ToggleHomebrewLikeCommand(id), ct)).ToResponse())
            .RequireAuthorization();

        return app;
    }
}
