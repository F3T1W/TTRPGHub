using MediatR;
using TTRPGHub.API.Extensions;
using TTRPGHub.Application.Features.Characters.Commands.CreateCharacter;
using TTRPGHub.Application.Features.Characters.Queries.GetCharacterById;
using TTRPGHub.Application.Features.Characters.Queries.GetMyCharacters;

namespace TTRPGHub.API.Endpoints.Characters;

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
            var result = await sender.Send(new GetCharacterByIdQuery(id), ct);
            return result.ToResponse();
        })
        .WithSummary("Получить персонажа по ID")
        .Produces<CharacterDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status401Unauthorized);
    }
}
