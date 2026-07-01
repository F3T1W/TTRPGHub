using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Features.Characters.Commands.CreateCharacterFromRules;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Queries.CalculateMulticlass;

// Самостоятельный калькулятор — не привязан к сущности Character (у неё один Class на всю жизнь
// персонажа, менять это рискованно для обратной совместимости, см. ROADMAP.md). Даёт игроку
// посчитать суммарный уровень/бонус мастерства/пул костей хитов для нескольких классов вручную.
internal sealed class CalculateMulticlassQueryHandler(
    IGameSystemRepository systemRepository,
    IRuleEntryRepository entryRepository
) : IRequestHandler<CalculateMulticlassQuery, Result<MulticlassResultDto>>
{
    public async Task<Result<MulticlassResultDto>> Handle(CalculateMulticlassQuery query, CancellationToken ct)
    {
        if (query.Classes.Count == 0)
            return Error.Validation("Multiclass.Empty", "Добавь хотя бы один класс.");

        var system = await systemRepository.GetBySlugAsync(query.SystemSlug, ct);
        if (system is null)
            return Error.NotFound("GameSystem");

        var results = new List<ClassLevelResultDto>();
        var hitDicePool = new List<string>();
        var totalLevel = 0;
        var isFirstClassLevel = true;

        foreach (var input in query.Classes)
        {
            if (input.Level < 1)
                return Error.Validation("Multiclass.InvalidLevel", "Уровень класса должен быть не меньше 1.");

            var classEntry = await entryRepository.GetBySlugAsync(system.Id, RuleCategory.Class, input.ClassSlug, ct);
            if (classEntry is null)
                return Error.NotFound($"Class:{input.ClassSlug}");

            var hitDice = ExtractHitDice(classEntry.StatsJson);
            var dieMax = CharacterAutomationCalculator.ParseHitDieMax(hitDice);

            // Первый уровень первого введённого класса — максимум кости (как при создании персонажа
            // 1 уровня), все остальные уровни (от любого класса) — среднее значение, по правилам мультикласса.
            var perLevelHp = isFirstClassLevel ? dieMax : dieMax / 2 + 1;
            var contribution = isFirstClassLevel
                ? dieMax + (input.Level - 1) * (dieMax / 2 + 1)
                : input.Level * (dieMax / 2 + 1);

            results.Add(new ClassLevelResultDto(classEntry.Title, input.Level, hitDice, contribution));
            hitDicePool.Add($"{input.Level}{hitDice[hitDice.IndexOf('d')..]} ({classEntry.Title})");

            totalLevel += input.Level;
            isFirstClassLevel = false;
            _ = perLevelHp; // используется только для читаемости расчёта выше
        }

        if (totalLevel > 20)
            return Error.Validation("Multiclass.TooHigh", "Суммарный уровень не может превышать 20.");

        var proficiencyBonus = totalLevel switch { <= 4 => 2, <= 8 => 3, <= 12 => 4, <= 16 => 5, _ => 6 };

        return new MulticlassResultDto(totalLevel, proficiencyBonus, results, hitDicePool);
    }

    private static string ExtractHitDice(string classStatsJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(classStatsJson);
            return doc.RootElement.TryGetProperty("hit_dice", out var v) && v.ValueKind == System.Text.Json.JsonValueKind.String
                ? v.GetString() ?? "1d8"
                : "1d8";
        }
        catch
        {
            return "1d8";
        }
    }
}
