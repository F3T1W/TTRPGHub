using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Characters.Commands.CreateCharacter;
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
    }
}
