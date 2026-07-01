using MediatR;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Extensions;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;
using TTRPGHub.Features.Characters.Commands.CreateCharacter;
using TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;
using TTRPGHub.Features.Characters.Commands.ImportCharacter;
using TTRPGHub.Features.Characters.Commands.LevelUpCharacter;
using TTRPGHub.Features.Characters.Commands.UpdateCharacter;
using TTRPGHub.Features.Characters.Commands.UploadAvatar;
using TTRPGHub.Features.Characters.Queries.GetCharacterDetail;
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
    }
}

internal sealed record LevelUpRequest(int NewLevel);
