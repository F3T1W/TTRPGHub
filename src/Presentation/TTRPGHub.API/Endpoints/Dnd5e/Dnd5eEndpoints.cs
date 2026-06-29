using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Dnd5e.Monsters.Queries.GetMonsterDetail;
using TTRPGHub.Features.Dnd5e.Monsters.Queries.GetMonsters;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpellDetail;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;

namespace TTRPGHub.Endpoints.Dnd5e;

public static class Dnd5eEndpoints
{
    public static void MapDnd5e(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dnd5e");

        group.MapGet("/spells", async (
            [AsParameters] SpellSearchParams p, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetDnd5eSpellsQuery(p.Search, p.School, p.Level, p.Class, p.Page, p.PageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/spells/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDnd5eSpellDetailQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/monsters", async (
            [AsParameters] MonsterSearchParams p, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetDnd5eMonstersQuery(p.Search, p.Type, p.Size, p.Cr, p.Page, p.PageSize), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });

        group.MapGet("/monsters/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDnd5eMonsterDetailQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        });
    }
}

public record SpellSearchParams(
    string? Search, string? School, int? Level, string? Class,
    int Page = 1, int PageSize = 30);

public record MonsterSearchParams(
    string? Search, string? Type, string? Size, string? Cr,
    int Page = 1, int PageSize = 30);
