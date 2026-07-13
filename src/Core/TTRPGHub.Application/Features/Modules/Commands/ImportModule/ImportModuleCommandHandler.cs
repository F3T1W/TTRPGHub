using System.Text.Json;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Modules.Shared;
using TTRPGHub.Features.Rules.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Modules.Commands.ImportModule;

internal sealed class ImportModuleCommandHandler(
    IMacroRepository macros,
    IGameSystemRepository systems,
    IRuleEntryRepository entries,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<ImportModuleCommand, Result<ImportModuleResponse>>
{
    private const int MaxMacros = 200;
    private const int MaxEntries = 500;

    public async Task<Result<ImportModuleResponse>> Handle(ImportModuleCommand request, CancellationToken ct)
    {
        ModuleManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<ModuleManifest>(
                request.ManifestJson, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch (JsonException)
        {
            return Error.Validation("Module.Import", "Не удалось разобрать файл — ожидается манифест модуля (JSON).");
        }

        if (manifest is null || string.IsNullOrWhiteSpace(manifest.Name))
            return Error.Validation("Module.Import", "В манифесте нет названия модуля.");
        if (manifest.Macros.Count > MaxMacros)
            return Error.Validation("Module.Import", $"Слишком много макросов в модуле (максимум {MaxMacros}).");
        if (manifest.RuleSystem?.Entries.Count > MaxEntries)
            return Error.Validation("Module.Import", $"Слишком много записей справочника в модуле (максимум {MaxEntries}).");

        var importedMacros = new List<Macro>();
        foreach (var m in manifest.Macros)
        {
            if (string.IsNullOrWhiteSpace(m.Name)) continue;
            var type = string.Equals(m.Type, "script", StringComparison.OrdinalIgnoreCase) ? MacroType.Script : MacroType.Chat;
            importedMacros.Add(Macro.Create(currentUser.Id, m.Name.Trim(), m.ImageUrl, type, m.Command));
        }
        if (importedMacros.Count > 0)
            await macros.AddRangeAsync(importedMacros, ct);

        string? systemSlug = null;
        var importedEntries = new List<RuleEntry>();
        if (manifest.RuleSystem is { Entries.Count: > 0 } ruleSystem)
        {
            var baseSlug = SlugGenerator.FromTitle(ruleSystem.Name);
            var slug = baseSlug;
            var suffix = 2;
            while (await systems.ExistsAsync(slug, ct))
                slug = $"{baseSlug}-{suffix++}";

            var system = GameSystem.CreateCustom(slug, ruleSystem.Name, currentUser.Id);
            await systems.AddAsync(system, ct);
            systemSlug = slug;

            foreach (var e in ruleSystem.Entries)
            {
                if (string.IsNullOrWhiteSpace(e.Title)) continue;
                if (!Enum.TryParse<RuleCategory>(e.Category, ignoreCase: true, out var category)) continue;

                var entrySlug = SlugGenerator.FromTitle(e.Title);
                var entrySuffix = 2;
                while (importedEntries.Any(x => x.Category == category && x.Slug == entrySlug))
                    entrySlug = $"{SlugGenerator.FromTitle(e.Title)}-{entrySuffix++}";

                importedEntries.Add(RuleEntry.Create(
                    system.Id, category, entrySlug, e.Title, e.Summary, e.ContentMarkdown,
                    string.IsNullOrWhiteSpace(e.StatsJson) ? "{}" : e.StatsJson,
                    e.Tags, isHomebrew: true, source: $"Модуль «{manifest.Name}»" + (manifest.Author is null ? "" : $" ({manifest.Author})")));
            }

            if (importedEntries.Count > 0)
                await entries.AddRangeAsync(importedEntries, ct);
        }

        if (importedMacros.Count == 0 && importedEntries.Count == 0)
            return Error.Validation("Module.Import", "В модуле не найдено ни макросов, ни записей справочника для импорта.");

        await unitOfWork.SaveChangesAsync(ct);

        return new ImportModuleResponse(importedMacros.Count, importedEntries.Count, systemSlug);
    }
}
