using System.IO.Compression;
using TTRPGHub.Common.Interfaces;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace TTRPGHub.Infrastructure.Pdf;

// M.1 — импорт купленного приключения (PDF) в приватный Journal/Scene своей сессии (см. ROADMAP).
// Два нетривиальных момента, подтверждённых на реальном файле (Paizo Free RPG Day PDF):
//
// 1. Текст: обычный Page.Text у PdfPig перемешивает слова из соседних колонок на одной
//    визуальной строке (двухколоночная вёрстка Paizo) — получается нечитаемая каша. Нужен
//    именно ContentOrderTextExtractor из пакета DocumentLayoutAnalysis — он восстанавливает
//    порядок чтения через геометрический анализ колонок, а не наивным сравнением X-координат.
//
// 2. Картинки: у изображений этого PDF цепочка фильтров [/FlateDecode, /DCTDecode] —
//    PdfPig.TryGetPng(...) не умеет их декодировать (возвращает false). Но RawBytes после
//    FlateDecode — это валидный JPEG-поток (DCTDecode == JPEG), просто PdfPig отдаёт его ДО
//    инфлейта. Вручную распаковываем zlib (RawBytes начинаются с сигнатуры 78 9C) и сохраняем
//    как обычный .jpg — декодировать JPEG самим не нужно, это уже готовый файл для браузера.
//    (M.1 smoke-регрессия нашла соседний случай: обычный /DCTDecode БЕЗ обёртки FlateDecode —
//    тогда RawBytes уже сам по себе валидный JPEG, инфлейтить нечего — см. TryDecodeJpeg.)
// public (не internal), как и другие клиентские импортёры (Foundry/Pathbuilder) — чтобы
// scripts/AdventureImportSmoke мог прогнать парсер напрямую без поднятия всего DI-контейнера.
public sealed class PdfPigAdventureParser : IAdventurePdfParser
{
    private const int MinImageWidth = 300;
    private const int MinImageHeight = 250;

    public IReadOnlyList<AdventurePdfPage> ParsePages(Stream pdfStream)
    {
        using var document = PdfDocument.Open(pdfStream);
        var pages = new List<AdventurePdfPage>();
        var seenImageHashes = new HashSet<string>();

        foreach (var page in document.GetPages())
        {
            var text = SafeExtractText(page);
            var images = ExtractImages(page, seenImageHashes);
            pages.Add(new AdventurePdfPage(page.Number, text, images));
        }

        return pages;
    }

    private static string SafeExtractText(Page page)
    {
        try { return ContentOrderTextExtractor.GetText(page); }
        catch { return page.Text; } // деградация на странный layout — лучше кривой текст, чем ничего
    }

    private static List<AdventurePdfImage> ExtractImages(Page page, HashSet<string> seenHashes)
    {
        var result = new List<AdventurePdfImage>();
        foreach (var image in page.GetImages())
        {
            if (image.WidthInSamples < MinImageWidth || image.HeightInSamples < MinImageHeight)
                continue;

            var decoded = TryDecodeImage(image);
            if (decoded is not { } d)
                continue;

            // Одна и та же декоративная иллюстрация нередко переиспользуется на нескольких
            // страницах книги (фон, разделители) — дедуп по хешу байтов, чтобы не плодить
            // одинаковые "сцены" из одной и той же картинки.
            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(d.Bytes));
            if (!seenHashes.Add(hash))
                continue;

            result.Add(new AdventurePdfImage(d.Bytes, d.ContentType, image.WidthInSamples, image.HeightInSamples));
        }

        return result;
    }

    // Возвращает и байты, и реальный content-type — TryGetPng отдаёт PNG, наш JPEG-фолбэк
    // ниже отдаёт JPEG; раньше это лейблилось как "image/jpeg" безусловно (найдено M.1
    // smoke-регрессией — PNG-путь с неверным лейблом не ломается визуально в браузере,
    // тот сам определяет формат по содержимому, но семантически было неверно).
    private static (byte[] Bytes, string ContentType)? TryDecodeImage(IPdfImage image)
    {
        if (image.TryGetPng(out var png))
            return (png, "image/png");

        var raw = image.RawBytes.ToArray();

        // Обычный /DCTDecode без обёртки Flate — RawBytes уже готовый JPEG, инфлейтить нечего.
        if (IsJpeg(raw))
            return (raw, "image/jpeg");

        try
        {
            using var msIn = new MemoryStream(raw);
            using var zlib = new ZLibStream(msIn, CompressionMode.Decompress);
            using var msOut = new MemoryStream();
            zlib.CopyTo(msOut);
            var inflated = msOut.ToArray();
            return IsJpeg(inflated) ? (inflated, "image/jpeg") : null;
        }
        catch { return null; }
    }

    private static bool IsJpeg(byte[] bytes) =>
        bytes.Length > 4 && bytes[0] == 0xFF && bytes[1] == 0xD8;
}
