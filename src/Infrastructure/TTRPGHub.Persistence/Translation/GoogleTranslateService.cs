using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using TTRPGHub.Common.Interfaces;

namespace TTRPGHub.Translation;

// Бесплатный неофициальный endpoint Google Translate (без ключа). Используется только
// для одноразового фонового перевода импортируемого справочника (Open5e — англоязычный
// источник), не в горячем пути пользовательских запросов. См. ROADMAP.md.
internal sealed class GoogleTranslateService(HttpClient http, ILogger<GoogleTranslateService> logger) : ITranslationService
{
    private const int MaxChunkChars = 1800;

    public async Task<string> TranslateAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length <= 1)
            return text;

        if (text.Length <= MaxChunkChars)
            return await TranslateChunkAsync(text, ct);

        // Разбиваем длинный текст по абзацам, чтобы не превышать лимит длины запроса
        // и не переводить куски по границе, ломающей markdown-структуру (таблицы, списки).
        var paragraphs = text.Split('\n');
        var sb = new StringBuilder();
        var buffer = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (buffer.Length + paragraph.Length + 1 > MaxChunkChars && buffer.Length > 0)
            {
                sb.Append(await TranslateChunkAsync(buffer.ToString(), ct));
                sb.Append('\n');
                buffer.Clear();
            }
            buffer.Append(paragraph).Append('\n');
        }
        if (buffer.Length > 0)
            sb.Append(await TranslateChunkAsync(buffer.ToString().TrimEnd('\n'), ct));

        return sb.ToString().TrimEnd('\n');
    }

    private async Task<string> TranslateChunkAsync(string text, CancellationToken ct)
    {
        try
        {
            var url = "https://translate.googleapis.com/translate_a/single" +
                      "?client=gtx&sl=en&tl=ru&dt=t&q=" + Uri.EscapeDataString(text);

            var response = await http.GetStringAsync(url, ct);
            using var doc = JsonDocument.Parse(response);

            var sb = new StringBuilder();
            foreach (var segment in doc.RootElement[0].EnumerateArray())
                sb.Append(segment[0].GetString());

            var translated = sb.ToString();
            return string.IsNullOrWhiteSpace(translated) ? text : translated;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось перевести фрагмент текста, оставляю оригинал");
            return text;
        }
    }
}
