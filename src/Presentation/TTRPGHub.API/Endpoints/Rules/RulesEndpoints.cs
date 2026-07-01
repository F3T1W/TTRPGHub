using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Characters.Queries.CalculateMulticlass;
using TTRPGHub.Features.Rules.Commands.CreateGameSystem;
using TTRPGHub.Features.Rules.Commands.CreateRuleEntry;
using TTRPGHub.Features.Rules.Commands.DeleteRuleEntry;
using TTRPGHub.Features.Rules.Commands.UpdateRuleEntry;
using TTRPGHub.Features.Rules.Queries.GetGameSystems;
using TTRPGHub.Features.Rules.Queries.GetRuleEntries;
using TTRPGHub.Features.Rules.Queries.GetRuleEntryDetail;

namespace TTRPGHub.Endpoints.Rules;

internal static class RulesEndpoints
{
    internal static void MapRulesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/rules").WithTags("Rules");

        group.MapGet("/systems", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetGameSystemsQuery(), ct);
            return result.ToResponse();
        })
        .WithSummary("Список игровых систем (официальных и кастомных)")
        .Produces<List<GameSystemDto>>(StatusCodes.Status200OK);

        group.MapGet("/{systemSlug}/{category}", async (
            string systemSlug, string category, string? search, int page = 1, int pageSize = 40,
            ISender sender = null!, CancellationToken ct = default) =>
        {
            if (!Enum.TryParse<RuleCategory>(category, ignoreCase: true, out var parsedCategory))
                return Result<RuleEntryPageDto>.Failure(Error.Validation("RuleCategory.Invalid", "Неизвестная категория справочника.")).ToResponse();

            var result = await sender.Send(new GetRuleEntriesQuery(systemSlug, parsedCategory, search, page, pageSize), ct);
            return result.ToResponse();
        })
        .WithSummary("Список записей справочника (категория в рамках системы)")
        .Produces<RuleEntryPageDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{systemSlug}/{category}/{slug}", async (
            string systemSlug, string category, string slug,
            ISender sender, CancellationToken ct) =>
        {
            if (!Enum.TryParse<RuleCategory>(category, ignoreCase: true, out var parsedCategory))
                return Result<RuleEntryDetailDto>.Failure(Error.Validation("RuleCategory.Invalid", "Неизвестная категория справочника.")).ToResponse();

            var result = await sender.Send(new GetRuleEntryDetailQuery(systemSlug, parsedCategory, slug), ct);
            return result.ToResponse();
        })
        .WithSummary("Детали записи справочника")
        .Produces<RuleEntryDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/systems", async (CreateGameSystemRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateGameSystemCommand(req.Name), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/rules/systems/{result.Value!.Slug}", result.Value)
                : result.ToResponse();
        })
        .RequireAuthorization()
        .WithSummary("Создать свою (кастомную) игровую систему");

        group.MapPost("/{systemSlug}/{category}", async (
            string systemSlug, string category, CreateRuleEntryRequest req,
            ISender sender, CancellationToken ct) =>
        {
            if (!Enum.TryParse<RuleCategory>(category, ignoreCase: true, out var parsedCategory))
                return Result<CreateRuleEntryResponse>.Failure(Error.Validation("RuleCategory.Invalid", "Неизвестная категория справочника.")).ToResponse();

            var result = await sender.Send(new CreateRuleEntryCommand(
                systemSlug, parsedCategory, req.Title, req.Summary, req.ContentMarkdown,
                req.StatsJson ?? "{}", req.Tags ?? []), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/rules/{systemSlug}/{category}/{result.Value!.Slug}", result.Value)
                : result.ToResponse();
        })
        .RequireAuthorization()
        .WithSummary("Добавить запись в свою систему");

        group.MapPut("/{systemSlug}/{category}/{slug}", async (
            string systemSlug, string category, string slug, CreateRuleEntryRequest req,
            ISender sender, CancellationToken ct) =>
        {
            if (!Enum.TryParse<RuleCategory>(category, ignoreCase: true, out var parsedCategory))
                return Result.Failure(Error.Validation("RuleCategory.Invalid", "Неизвестная категория справочника.")).ToResponse();

            var result = await sender.Send(new UpdateRuleEntryCommand(
                systemSlug, parsedCategory, slug, req.Title, req.Summary, req.ContentMarkdown,
                req.StatsJson ?? "{}", req.Tags ?? []), ct);
            return result.ToResponse();
        })
        .RequireAuthorization()
        .WithSummary("Изменить запись в своей системе");

        group.MapDelete("/{systemSlug}/{category}/{slug}", async (
            string systemSlug, string category, string slug,
            ISender sender, CancellationToken ct) =>
        {
            if (!Enum.TryParse<RuleCategory>(category, ignoreCase: true, out var parsedCategory))
                return Result.Failure(Error.Validation("RuleCategory.Invalid", "Неизвестная категория справочника.")).ToResponse();

            var result = await sender.Send(new DeleteRuleEntryCommand(systemSlug, parsedCategory, slug), ct);
            return result.ToResponse();
        })
        .RequireAuthorization()
        .WithSummary("Удалить запись из своей системы");

        group.MapPost("/{systemSlug}/multiclass", async (
            string systemSlug, List<ClassLevelInput> classes, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CalculateMulticlassQuery(systemSlug, classes), ct);
            return result.ToResponse();
        })
        .WithSummary("Калькулятор мультикласса: суммарный уровень, бонус мастерства, пул костей хитов")
        .Produces<MulticlassResultDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }
}

internal sealed record CreateGameSystemRequest(string Name);

internal sealed record CreateRuleEntryRequest(
    string Title, string? Summary, string? ContentMarkdown, string? StatsJson, string[]? Tags);
