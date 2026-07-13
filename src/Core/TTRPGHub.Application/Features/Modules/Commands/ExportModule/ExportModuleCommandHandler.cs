using System.Text.Json;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Modules.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Modules.Commands.ExportModule;

internal sealed class ExportModuleCommandHandler(
    IMacroRepository macros,
    IGameSystemRepository systems,
    IRuleEntryRepository entries,
    IUserRepository users,
    ICurrentUser currentUser
) : IRequestHandler<ExportModuleCommand, Result<string>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public async Task<Result<string>> Handle(ExportModuleCommand request, CancellationToken ct)
    {
        var myMacros = await macros.GetByOwnerAsync(currentUser.Id, ct);
        var selectedMacros = request.MacroIds.Count == 0
            ? []
            : myMacros.Where(m => request.MacroIds.Contains(m.Id))
                .Select(m => new ModuleMacro(m.Name, m.ImageUrl, m.Type.ToString(), m.Command))
                .ToList();

        ModuleRuleSystem? ruleSystem = null;
        if (!string.IsNullOrWhiteSpace(request.SystemSlug))
        {
            var system = await systems.GetBySlugAsync(request.SystemSlug, ct);
            if (system is null)
                return Error.NotFound("GameSystem");
            if (system.IsOfficial || system.CreatedByUserId != currentUser.Id)
                return Error.Forbidden();

            var systemEntries = await entries.GetAllBySystemAsync(system.Id, ct);
            ruleSystem = new ModuleRuleSystem(
                system.Name,
                systemEntries.Select(e => new ModuleRuleEntry(
                    e.Category.ToString(), e.Title, e.Summary, e.ContentMarkdown, e.StatsJson, e.Tags)).ToList());
        }

        if (selectedMacros.Count == 0 && ruleSystem is null)
            return Error.Validation("Module", "Модуль пуст — выбери хотя бы один макрос или свою систему справочника.");

        var author = await users.GetByIdAsync(currentUser.Id, ct);
        var manifest = new ModuleManifest(
            FormatVersion: 1,
            Name: request.Name,
            Description: request.Description,
            Author: author?.Username,
            Version: string.IsNullOrWhiteSpace(request.Version) ? "1.0.0" : request.Version,
            Macros: selectedMacros,
            RuleSystem: ruleSystem);

        return JsonSerializer.Serialize(manifest, JsonOptions);
    }
}
