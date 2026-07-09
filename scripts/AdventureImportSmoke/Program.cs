using AdventureImportSmoke;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TTRPGHub.Infrastructure.Pdf;

QuestPDF.Settings.License = LicenseType.Community;

// M.1 regression smoke — синтетический PDF (не копирайтный контент, генерируется этим же
// скриптом), воспроизводящий структурные особенности, из-за которых наивный парсинг ломался
// на реальном купленном приключении: двухколоночная вёрстка (наивная эвристика "midpoint
// страницы" даёт кашу — нужен ContentOrderTextExtractor) + встроенное растровое изображение
// (нужно, чтобы ExtractImages реально что-то извлекала, а не просто не падала на пустом месте).
const string leftColumnMarker = "LEFTCOLUMNSTART Alpha Bravo Charlie Delta Echo Foxtrot Golf Hotel LEFTCOLUMNEND";
const string rightColumnMarker = "RIGHTCOLUMNSTART Kilo Lima Mike November Oscar Papa Quebec Romeo RIGHTCOLUMNEND";

var pdfBytes = Document.Create(container =>
{
    container.Page(page =>
    {
        page.Size(PageSizes.A4);
        page.Margin(30);
        page.Content().Column(column =>
        {
            column.Item().Row(row =>
            {
                row.RelativeItem().Text(leftColumnMarker).FontSize(11);
                row.RelativeItem().Text(rightColumnMarker).FontSize(11);
            });
            var mapImageBytes = MinimalPngWriter.CreateSolidColorPng(320, 260, 90, 60, 30);
            column.Item().PaddingTop(20).Image(mapImageBytes);
        });
    });
}).GeneratePdf();

using var stream = new MemoryStream(pdfBytes);
var parser = new PdfPigAdventureParser();
var pages = parser.ParsePages(stream);

var checks = new List<(string Name, bool Ok)>();

var page1 = pages.FirstOrDefault();
checks.Add(("page count == 1", pages.Count == 1));
checks.Add(("text contains left column", page1?.Text.Contains("LEFTCOLUMNSTART") == true && page1.Text.Contains("LEFTCOLUMNEND")));
checks.Add(("text contains right column", page1?.Text.Contains("RIGHTCOLUMNSTART") == true && page1.Text.Contains("RIGHTCOLUMNEND")));

// Главная регрессия, которую защищает этот тест: без ContentOrderTextExtractor слова из
// правой колонки перемешиваются ВНУТРИ левой (см. историю — "78 9C" наивная эвристика).
// Проверяем, что весь левый маркер идёт одним связным куском, а не раздроблен словами справа.
var normalized = page1?.Text.Replace("\n", " ").Replace("  ", " ") ?? "";
var leftBlockIntact = normalized.Contains(
    "LEFTCOLUMNSTART Alpha Bravo Charlie Delta Echo Foxtrot Golf Hotel LEFTCOLUMNEND");
checks.Add(("left column words stay in order, uninterrupted by right column", leftBlockIntact));

checks.Add(("at least one image extracted", page1?.Images.Count > 0));
if (page1?.Images.Count > 0)
{
    var img = page1.Images[0];
    checks.Add(("image dimensions match generated source image (320x260)", img.Width == 320 && img.Height == 260));

    // QuestPDF решает сам, как перекодировать встроенную картинку (PNG на входе не гарантирует
    // PNG внутри готового PDF — часто переупаковывается в JPEG) — проверяем самосогласованность
    // заявленного ContentType с реальной сигнатурой байтов, а не конкретный формат.
    var isPng = img.Bytes.Length > 8 && img.Bytes[0] == 0x89 && img.Bytes[1] == 0x50 && img.Bytes[2] == 0x4E && img.Bytes[3] == 0x47;
    var isJpeg = img.Bytes.Length > 2 && img.Bytes[0] == 0xFF && img.Bytes[1] == 0xD8;
    checks.Add(("image bytes look like a valid PNG or JPEG", isPng || isJpeg));
    checks.Add(("reported ContentType matches actual byte signature (regression: was hardcoded image/jpeg)",
        (isPng && img.ContentType == "image/png") || (isJpeg && img.ContentType == "image/jpeg")));
}

var failed = checks.Where(c => !c.Ok).ToList();
foreach (var c in checks)
    Console.WriteLine(c.Ok ? $"OK  {c.Name}" : $"FAIL {c.Name}");

if (failed.Count > 0)
{
    Console.WriteLine($"\n{failed.Count} check(s) failed.");
    if (page1 is not null) Console.WriteLine("--- extracted text ---\n" + page1.Text);
    return 1;
}

Console.WriteLine("\nAll adventure PDF parser checks passed.");
return 0;
