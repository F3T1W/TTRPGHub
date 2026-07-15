using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TTRPGHub.Infrastructure.Pdf;

namespace TTRPGHub.Infrastructure.Tests;

public class PdfPigAdventureParserTests
{
    static PdfPigAdventureParserTests() => QuestPDF.Settings.License = LicenseType.Community;

    private readonly PdfPigAdventureParser _parser = new();

    private static byte[] BuildPdf(params string[] pageTexts) =>
        Document.Create(container =>
        {
            foreach (var text in pageTexts)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(20);
                    page.Content().Text(text);
                });
            }
        }).GeneratePdf();

    [Fact]
    public void ParsePages_SinglePageDocument_ReturnsOnePage()
    {
        var pdf = BuildPdf("The goblins ambush the party at dusk.");
        using var stream = new MemoryStream(pdf);

        var pages = _parser.ParsePages(stream);

        Assert.Single(pages);
    }

    [Fact]
    public void ParsePages_ExtractsPageText()
    {
        var pdf = BuildPdf("The goblins ambush the party at dusk.");
        using var stream = new MemoryStream(pdf);

        var pages = _parser.ParsePages(stream);

        Assert.Contains("goblins ambush the party", pages[0].Text);
    }

    [Fact]
    public void ParsePages_PageNumbersAreOneBasedAndInOrder()
    {
        var pdf = BuildPdf("First page", "Second page", "Third page");
        using var stream = new MemoryStream(pdf);

        var pages = _parser.ParsePages(stream);

        Assert.Equal([1, 2, 3], pages.Select(p => p.PageNumber));
    }

    [Fact]
    public void ParsePages_MultiPageDocument_ExtractsEachPagesOwnText()
    {
        var pdf = BuildPdf("First page content", "Second page content");
        using var stream = new MemoryStream(pdf);

        var pages = _parser.ParsePages(stream);

        Assert.Contains("First page content", pages[0].Text);
        Assert.Contains("Second page content", pages[1].Text);
        Assert.DoesNotContain("Second page content", pages[0].Text);
    }

    [Fact]
    public void ParsePages_PageWithNoImages_ReturnsEmptyImageList()
    {
        var pdf = BuildPdf("No pictures here.");
        using var stream = new MemoryStream(pdf);

        var pages = _parser.ParsePages(stream);

        Assert.Empty(pages[0].Images);
    }

    [Fact]
    public void ParsePages_BlankPage_ReturnsEmptyText()
    {
        var pdf = BuildPdf(string.Empty);
        using var stream = new MemoryStream(pdf);

        var pages = _parser.ParsePages(stream);

        Assert.Single(pages);
        Assert.Equal(string.Empty, pages[0].Text.Trim());
    }

    private static bool InvokeIsJpeg(byte[] bytes)
    {
        var method = typeof(PdfPigAdventureParser).GetMethod(
            "IsJpeg", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (bool)method.Invoke(null, [bytes])!;
    }

    [Fact]
    public void IsJpeg_ValidJpegMagicBytes_ReturnsTrue()
    {
        Assert.True(InvokeIsJpeg([0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10]));
    }

    [Fact]
    public void IsJpeg_PngMagicBytes_ReturnsFalse()
    {
        Assert.False(InvokeIsJpeg([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A]));
    }

    [Fact]
    public void IsJpeg_TooShortArray_ReturnsFalse()
    {
        Assert.False(InvokeIsJpeg([0xFF, 0xD8]));
    }

    [Fact]
    public void IsJpeg_EmptyArray_ReturnsFalse()
    {
        Assert.False(InvokeIsJpeg([]));
    }
}
