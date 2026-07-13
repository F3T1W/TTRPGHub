using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Modules.Commands.ExportModule;
using TTRPGHub.Features.Modules.Commands.ImportModule;

namespace TTRPGHub.Endpoints.Modules;

public static class ModulesEndpoints
{
    public static void MapModules(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/modules").WithTags("Modules").RequireAuthorization();

        group.MapPost("/export", async (ExportModuleRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ExportModuleCommand(
                req.Name, req.Description, req.Version, req.MacroIds, req.SystemSlug), ct);
            return result.ToResponse();
        })
        .WithSummary("Собрать модуль (макросы + своя система справочника) в JSON-манифест для скачивания");

        group.MapPost("/import", async (IFormFile file, ISender sender, CancellationToken ct) =>
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync(ct);
            var result = await sender.Send(new ImportModuleCommand(content), ct);
            return result.ToResponse();
        })
        .WithSummary("Импортировать модуль из JSON-манифеста")
        .DisableAntiforgery();
    }
}

internal sealed record ExportModuleRequest(
    string Name, string? Description, string? Version, List<Guid> MacroIds, string? SystemSlug);
