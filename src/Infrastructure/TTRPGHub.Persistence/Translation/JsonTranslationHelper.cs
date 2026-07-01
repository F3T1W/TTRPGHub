using System.Text.Json;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Translation;

// Рекурсивно проходит по произвольной JSON-структуре (объекты/массивы) и переводит
// каждую строковую "листовую" ценность. Числа, bool и null не трогает.
internal static class JsonTranslationHelper
{
    public static async Task<string> TranslateJsonAsync(
        ITranslationService translator, string json,
        IReadOnlySet<string>? excludeKeys = null, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(json);
        var translated = await TranslateElementAsync(translator, doc.RootElement, excludeKeys, ct);
        return JsonSerializer.Serialize(translated);
    }

    private static async Task<object?> TranslateElementAsync(
        ITranslationService translator, JsonElement element, IReadOnlySet<string>? excludeKeys, CancellationToken ct)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    // Ключи вроде "hit_dice" содержат игровую нотацию ("1d12"), а не прозу —
                    // машинный перевод иногда портит латинскую 'd' на кириллическую 'д', ломая
                    // regex-парсинг в CharacterAutomationCalculator. Не переводим такие поля.
                    if (excludeKeys is not null && excludeKeys.Contains(prop.Name))
                        obj[prop.Name] = RawValue(prop.Value);
                    else
                        obj[prop.Name] = await TranslateElementAsync(translator, prop.Value, excludeKeys, ct);
                }
                return obj;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                    list.Add(await TranslateElementAsync(translator, item, excludeKeys, ct));
                return list;

            case JsonValueKind.String:
                var text = element.GetString() ?? "";
                return await translator.TranslateAsync(text, ct);

            case JsonValueKind.Number:
                return element.TryGetInt64(out var l) ? l : element.GetDouble();

            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;

            default:
                return null;
        }
    }

    private static object? RawValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Object or JsonValueKind.Array => JsonSerializer.Deserialize<object>(element.GetRawText()),
        _ => null
    };
}
