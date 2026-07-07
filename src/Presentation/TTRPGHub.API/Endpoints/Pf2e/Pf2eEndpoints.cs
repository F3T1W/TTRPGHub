using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Pf2e.Monsters.Queries.GetMonsterDetail;
using TTRPGHub.Features.Pf2e.Monsters.Queries.GetMonsters;
using TTRPGHub.Features.Pf2e.Spells.Queries.GetSpellDetail;
using TTRPGHub.Features.Pf2e.Spells.Queries.GetSpells;

namespace TTRPGHub.Endpoints.Pf2e;

public static class Pf2eEndpoints
{
    public static void MapPf2e(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pf2e");

        group.MapGet("/spells", async (
            [AsParameters] Pf2eSpellSearchParams p, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetPf2eSpellsQuery(p.Search, p.Tradition, p.Level, p.Trait, p.Page, p.PageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/spells/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPf2eSpellDetailQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/monsters", async (
            [AsParameters] Pf2eMonsterSearchParams p, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetPf2eMonstersQuery(p.Search, p.Trait, p.Size, p.Level, p.Page, p.PageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/monsters/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPf2eMonsterDetailQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        // K.3 — уникальный токен-арт: SVG детерминирован по данным монстра, поэтому кешируется
        // навечно (immutable). Без авторизации намеренно: картинка грузится через
        // background-image/<img>, куда Authorization-заголовок не подставить.
        group.MapGet("/monsters/{id:guid}/token.svg", async (Guid id, HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetPf2eMonsterDetailQuery(id), ct);
            if (!result.IsSuccess)
                return result.ToResponse();

            http.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
            var monster = result.Value!;
            var svg = Services.Pf2eTokenArtGenerator.GenerateSvg(monster.Slug, monster.Name, monster.Traits);
            return Results.Content(svg, "image/svg+xml");
        });
    }
}

public record Pf2eSpellSearchParams(
    string? Search, string? Tradition, int? Level, string? Trait,
    int Page = 1, int PageSize = 30);

public record Pf2eMonsterSearchParams(
    string? Search, string? Trait, string? Size, int? Level,
    int Page = 1, int PageSize = 30);
