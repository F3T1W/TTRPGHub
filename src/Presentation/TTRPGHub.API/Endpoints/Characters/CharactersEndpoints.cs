using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Extensions;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Pf2e;
using TTRPGHub.Features.Characters.Commands.CreateCharacter;
using TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;
using TTRPGHub.Features.Characters.Commands.CreateChronicle;
using TTRPGHub.Features.Characters.Commands.CreateCompanion;
using TTRPGHub.Features.Characters.Commands.DeleteChronicle;
using TTRPGHub.Features.Characters.Commands.DeleteCompanion;
using TTRPGHub.Features.Characters.Commands.ImportCharacter;
using TTRPGHub.Features.Characters.Commands.LevelUpCharacter;
using TTRPGHub.Features.Characters.Commands.UpdateCharacter;
using TTRPGHub.Features.Characters.Commands.UpdateCharacterPf2eStats;
using TTRPGHub.Features.Characters.Commands.UpdateCompanion;
using TTRPGHub.Features.Characters.Commands.UploadAvatar;
using TTRPGHub.Features.Characters.Queries.GetCharacterDetail;
using TTRPGHub.Features.Characters.Queries.GetChronicles;
using TTRPGHub.Features.Characters.Queries.GetCompanionById;
using TTRPGHub.Features.Characters.Queries.GetCompanions;
using TTRPGHub.Features.Characters.Queries.GetMyCharacters;

namespace TTRPGHub.Endpoints.Characters;

internal static class CharactersEndpoints
{
    internal static void MapCharactersEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/characters")
            .WithTags("Characters")
            .RequireAuthorization();

        group.MapPost("/", async (CreateCharacterCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/characters/{result.Value!.CharacterId}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Создать персонажа")
        .Produces<CreateCharacterResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/from-rules", async (CreateCharacterFromRulesCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/characters/{result.Value!.CharacterId}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Создать персонажа мастером (авто-расчёт по расе/классу из справочника)")
        .Produces<CreateCharacterFromRulesResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyCharactersQuery(), ct);
            return result.ToResponse();
        })
        .WithSummary("Мои персонажи")
        .Produces<IReadOnlyList<CharacterSummaryDto>>(StatusCodes.Status200OK);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCharacterDetailQuery(id), ct);
            return result.ToResponse();
        })
        .WithSummary("Полный лист персонажа")
        .Produces<CharacterDetailDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCharacterCommand command, ISender sender, CancellationToken ct) =>
        {
            if (id != command.CharacterId)
                return Results.BadRequest("ID в URL не совпадает с телом запроса.");

            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Обновить лист персонажа")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}/pf2e-stats", async (Guid id, UpdatePf2eStatsRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateCharacterPf2eStatsCommand(id, req.StatsJson), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Обновить PF2e-лист персонажа (ранги владения, спеллкастинг, инвентарь)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/level-up", async (Guid id, LevelUpRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LevelUpCharacterCommand(id, req.NewLevel), ct);
            return result.ToResponse();
        })
        .WithSummary("Повысить уровень персонажа (авто-пересчёт HP + подсказка что нового)")
        .Produces<LevelUpResponse>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPost("/{id:guid}/avatar", async (Guid id, IFormFile file, ISender sender, CancellationToken ct) =>
        {
            var command = new UploadAvatarCommand(
                id, file.OpenReadStream(), file.FileName, file.ContentType, file.Length);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Ok(new { url = result.Value })
                : result.ToResponse();
        })
        .WithSummary("Загрузить аватар персонажа")
        .Produces<object>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity)
        .DisableAntiforgery();

        group.MapGet("/{id:guid}/pdf", async (
            Guid id, ISender sender,
            ICharacterPdfService pdfService,
            ICharacterRepository characterRepo,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCharacterDetailQuery(id), ct);
            if (!result.IsSuccess) return result.ToResponse();

            var character = await characterRepo.GetByIdAsync(new CharacterId(id), ct);
            if (character is null) return Results.NotFound();

            byte[]? avatarBytes = null;
            if (!string.IsNullOrEmpty(character.AvatarUrl))
            {
                try
                {
                    using var http = new HttpClient();
                    avatarBytes = await http.GetByteArrayAsync(character.AvatarUrl, ct);
                }
                catch { /* аватарка недоступна — генерируем без неё */ }
            }

            var bytes = pdfService.Generate(character, avatarBytes);
            var filename = $"{character.Name.Replace(" ", "_")}.pdf";
            return Results.File(bytes, "application/pdf", filename);
        })
        .WithSummary("Скачать лист персонажа в PDF")
        .AllowAnonymous();

        group.MapPost("/import", async (ImportCharacterCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/characters/{result.Value!.CharacterId}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Импортировать персонажа из JSON");

        // N.3 — Pathfinder Society Chronicle Sheets
        group.MapGet("/{id:guid}/chronicles", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetChroniclesQuery(id), ct);
            return result.ToResponse();
        })
        .WithSummary("Хроники Pathfinder Society персонажа")
        .Produces<IReadOnlyList<ChronicleDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/chronicles", async (Guid id, CreateChronicleRequest req, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateChronicleCommand(
                id, req.ScenarioName, req.SessionDate, req.GmName, req.Faction,
                req.GoldEarned, req.AchievementPoints, req.BoonsUsed, req.Notes);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/characters/{id}/chronicles/{result.Value!.ChronicleId}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Добавить chronicle sheet за сыгранный сценарий")
        .Produces<CreateChronicleResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:guid}/chronicles/{chronicleId:guid}", async (Guid id, Guid chronicleId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteChronicleCommand(chronicleId), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Удалить chronicle sheet")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapGet("/{id:guid}/chronicles/{chronicleId:guid}/pdf", async (
            Guid id, Guid chronicleId,
            ISender sender,
            IChroniclePdfService pdfService,
            ICharacterRepository characterRepo,
            IPathfinderSocietyChronicleRepository chronicleRepo,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new GetChroniclesQuery(id), ct);
            if (!result.IsSuccess) return result.ToResponse();

            var character = await characterRepo.GetByIdAsync(new CharacterId(id), ct);
            if (character is null) return Results.NotFound();

            var chronicle = await chronicleRepo.GetByIdAsync(new PathfinderSocietyChronicleId(chronicleId), ct);
            if (chronicle is null || chronicle.CharacterId != character.Id) return Results.NotFound();

            var bytes = pdfService.Generate(character, chronicle);
            var filename = $"{character.Name.Replace(" ", "_")}_chronicle_{chronicle.SessionDate:yyyy-MM-dd}.pdf";
            return Results.File(bytes, "application/pdf", filename);
        })
        .WithSummary("Скачать chronicle sheet в PDF")
        .AllowAnonymous();

        // N.8 — Companion/Familiar/Animal Companion листы
        group.MapGet("/{id:guid}/companions", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCompanionsQuery(id), ct);
            return result.ToResponse();
        })
        .WithSummary("Компаньоны/фамильяры персонажа")
        .Produces<IReadOnlyList<CompanionDto>>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id:guid}/companions", async (Guid id, CreateCompanionRequest req, ISender sender, CancellationToken ct) =>
        {
            var command = new CreateCompanionCommand(
                id, req.Name, req.Kind, req.Level, req.MaxHitPoints,
                req.ArmorClass, req.Speed, req.AttacksText, req.AbilitiesText, req.Notes);
            var result = await sender.Send(command, ct);
            return result.IsSuccess
                ? Results.Created($"/api/characters/{id}/companions/{result.Value!.CompanionId}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Добавить компаньона/фамильяра")
        .Produces<CreateCompanionResponse>(StatusCodes.Status201Created)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapPut("/{id:guid}/companions/{companionId:guid}", async (Guid id, Guid companionId, UpdateCompanionRequest req, ISender sender, CancellationToken ct) =>
        {
            var command = new UpdateCompanionCommand(
                companionId, req.Name, req.Kind, req.Level, req.MaxHitPoints, req.CurrentHitPoints,
                req.ArmorClass, req.Speed, req.AttacksText, req.AbilitiesText, req.Notes);
            var result = await sender.Send(command, ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Обновить компаньона/фамильяра (статы, текущие ХП)")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/{id:guid}/companions/{companionId:guid}", async (Guid id, Guid companionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteCompanionCommand(companionId), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        })
        .WithSummary("Удалить компаньона/фамильяра")
        .Produces(StatusCodes.Status204NoContent)
        .ProducesProblem(StatusCodes.Status404NotFound);

        // N.8 — жетон компаньона на столе знает только CompanionId, не CharacterId владельца
        // (см. GetCompanionByIdQuery) — отдельный маршрут вне /api/characters/{id}/...
        app.MapGet("/api/companions/{companionId:guid}", async (Guid companionId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetCompanionByIdQuery(companionId), ct);
            return result.ToResponse();
        })
        .WithTags("Characters")
        .RequireAuthorization()
        .WithSummary("Компаньон по ID (для токена на столе)")
        .Produces<CompanionDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound);
    }
}

internal sealed record LevelUpRequest(int NewLevel);
internal sealed record UpdatePf2eStatsRequest(string StatsJson);

internal sealed record CreateChronicleRequest(
    string ScenarioName, DateOnly SessionDate, string? GmName, string? Faction,
    int GoldEarned, int AchievementPoints, string? BoonsUsed, string? Notes);

internal sealed record CreateCompanionRequest(
    string Name, string Kind, int Level, int MaxHitPoints, int? ArmorClass,
    string? Speed, string? AttacksText, string? AbilitiesText, string? Notes);

internal sealed record UpdateCompanionRequest(
    string Name, string Kind, int Level, int MaxHitPoints, int CurrentHitPoints, int? ArmorClass,
    string? Speed, string? AttacksText, string? AbilitiesText, string? Notes);
