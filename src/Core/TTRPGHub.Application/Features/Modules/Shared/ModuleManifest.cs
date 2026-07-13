namespace TTRPGHub.Features.Modules.Shared;

// Формат файла модуля (J.9). Версия формата (не версия самого модуля) — на случай, если
// структура манифеста изменится в будущем и потребуется миграция старых файлов при импорте.
public sealed record ModuleManifest(
    int FormatVersion,
    string Name,
    string? Description,
    string? Author,
    string? Version,
    List<ModuleMacro> Macros,
    ModuleRuleSystem? RuleSystem);

public sealed record ModuleMacro(string Name, string? ImageUrl, string Type, string Command);

public sealed record ModuleRuleSystem(string Name, List<ModuleRuleEntry> Entries);

public sealed record ModuleRuleEntry(
    string Category, string Title, string? Summary, string? ContentMarkdown,
    string StatsJson, string[] Tags);
