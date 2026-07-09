using System.Text.Json;
using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Macros.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Macros.Commands.ImportFoundryMacros;

internal sealed class ImportFoundryMacrosCommandHandler(
    IMacroRepository macroRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser
) : IRequestHandler<ImportFoundryMacrosCommand, Result<List<MacroDto>>>
{
    private sealed record FoundryMacro(string? Name, string? Type, string? Img, string? Command);

    public async Task<Result<List<MacroDto>>> Handle(ImportFoundryMacrosCommand command, CancellationToken ct)
    {
        List<FoundryMacro> parsed;
        try
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            var trimmed = command.FileContent.TrimStart();
            parsed = trimmed.StartsWith('[')
                ? JsonSerializer.Deserialize<List<FoundryMacro>>(command.FileContent, options) ?? []
                : [JsonSerializer.Deserialize<FoundryMacro>(command.FileContent, options)!];
        }
        catch (JsonException)
        {
            return Error.Validation("Macro.Import", "Не удалось разобрать файл — ожидается JSON-экспорт макроса или массива макросов Foundry.");
        }

        var imported = new List<Macro>();
        foreach (var m in parsed)
        {
            if (string.IsNullOrWhiteSpace(m.Name)) continue;
            var type = string.Equals(m.Type, "script", StringComparison.OrdinalIgnoreCase) ? MacroType.Script : MacroType.Chat;
            imported.Add(Macro.Create(currentUser.Id, m.Name.Trim(), m.Img, type, m.Command ?? ""));
        }

        if (imported.Count == 0)
            return Error.Validation("Macro.Import", "В файле не найдено ни одного макроса с именем.");

        await macroRepository.AddRangeAsync(imported, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return imported.Select(MacroMapper.ToDto).ToList();
    }
}
