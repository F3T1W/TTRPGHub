using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Macros.Commands.CreateMacro;
using TTRPGHub.Features.Macros.Commands.DeleteMacro;
using TTRPGHub.Features.Macros.Commands.ImportFoundryMacros;
using TTRPGHub.Features.Macros.Commands.SetMacroHotbarSlot;
using TTRPGHub.Features.Macros.Commands.UpdateMacro;
using TTRPGHub.Features.Macros.Queries.GetMyMacros;

namespace TTRPGHub.Endpoints.Macros;

public static class MacrosEndpoints
{
    public static void MapMacros(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/macros").WithTags("Macros").RequireAuthorization();

        group.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetMyMacrosQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        })
        .WithSummary("Личная библиотека макросов текущего пользователя");

        group.MapPost("/", async (CreateMacroRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new CreateMacroCommand(request.Name, request.ImageUrl, request.Type, request.Command), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/macros/{result.Value!.Id}", result.Value)
                : result.ToResponse();
        })
        .WithSummary("Создать макрос");

        group.MapPut("/{id:guid}", async (Guid id, UpdateMacroRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new UpdateMacroCommand(id, request.Name, request.ImageUrl, request.Type, request.Command), ct);
            return result.ToResponse();
        })
        .WithSummary("Изменить макрос");

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeleteMacroCommand(id), ct);
            return result.ToResponse();
        })
        .WithSummary("Удалить макрос");

        group.MapPut("/{id:guid}/hotbar-slot", async (Guid id, SetHotbarSlotRequest request, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetMacroHotbarSlotCommand(id, request.Slot), ct);
            return result.ToResponse();
        })
        .WithSummary("Назначить/снять макрос со слота хотбара (0-9, -1 = снять)");

        group.MapPost("/import/foundry", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync(ct);
            var result = await sender.Send(new ImportFoundryMacrosCommand(content), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        })
        .WithSummary("Импортировать макрос(ы), экспортированные из Foundry VTT (JSON)")
        .DisableAntiforgery();
    }
}

internal sealed record CreateMacroRequest(string Name, string? ImageUrl, string Type, string Command);
internal sealed record UpdateMacroRequest(string Name, string? ImageUrl, string Type, string Command);
internal sealed record SetHotbarSlotRequest(int Slot);
