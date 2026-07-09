using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Pf2e;

namespace TTRPGHub.Infrastructure.Pdf;

internal sealed class ChroniclePdfService : IChroniclePdfService
{
    private const string Accent = "#7c3aed";
    private const string AccentLight = "#a78bfa";
    private const string Gold = "#f59e0b";
    private const string Bg = "#0d0d1a";
    private const string Surface = "#13132a";
    private const string TextPrimary = "#ffffff";
    private const string TextMuted = "#9ca3af";
    private const string Border = "#2d2d4e";

    public byte[] Generate(Character c, PathfinderSocietyChronicle chronicle)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5.Landscape());
                page.Margin(20);
                page.PageColor(Bg);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontColor(TextPrimary).FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Element(e => RenderHeader(e, chronicle));
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(e => RenderCharacter(e, c));
                        row.ConstantItem(10);
                        row.RelativeItem().Element(e => RenderRewards(e, chronicle));
                    });
                    if (!string.IsNullOrWhiteSpace(chronicle.BoonsUsed))
                        col.Item().Element(e => RenderTextBlock(e, "ИСПОЛЬЗОВАННЫЕ БУНЫ", chronicle.BoonsUsed));
                    if (!string.IsNullOrWhiteSpace(chronicle.Notes))
                        col.Item().Element(e => RenderTextBlock(e, "ЗАМЕТКИ ГМ", chronicle.Notes));
                });
            });
        }).GeneratePdf();
    }

    private static void RenderHeader(IContainer e, PathfinderSocietyChronicle chronicle)
    {
        e.Background(Surface).Border(1).BorderColor(Accent).Padding(12).Row(row =>
        {
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("CHRONICLE SHEET").FontSize(9).FontColor(TextMuted);
                col.Item().Text(chronicle.ScenarioName).FontSize(18).Bold().FontColor(AccentLight);
            });
            row.ConstantItem(150).Column(col =>
            {
                col.Item().AlignRight().Text(chronicle.SessionDate.ToString("dd.MM.yyyy")).FontSize(11).FontColor(TextPrimary);
                if (!string.IsNullOrWhiteSpace(chronicle.GmName))
                    col.Item().AlignRight().Text($"ГМ: {chronicle.GmName}").FontSize(9).FontColor(TextMuted);
            });
        });
    }

    private static void RenderCharacter(IContainer e, Character c)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(6).Text("ПЕРСОНАЖ").FontSize(8).Bold().FontColor(AccentLight);
            col.Item().Text(c.Name).FontSize(13).Bold().FontColor(TextPrimary);
            col.Item().Text($"{c.Race} · {c.Class} · {c.Level} уровень").FontSize(9).FontColor(TextMuted);
        });
    }

    private static void RenderRewards(IContainer e, PathfinderSocietyChronicle chronicle)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(6).Text("НАГРАДЫ").FontSize(8).Bold().FontColor(AccentLight);
            RewardRow(col, "Золото", $"{chronicle.GoldEarned} зм");
            RewardRow(col, "Achievement Points", chronicle.AchievementPoints.ToString());
            if (!string.IsNullOrWhiteSpace(chronicle.Faction))
                RewardRow(col, "Фракция", chronicle.Faction);
        });
    }

    private static void RewardRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(3).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(9).FontColor(TextMuted);
            row.ConstantItem(80).AlignRight().Text(value).FontSize(10).Bold().FontColor(Gold);
        });
    }

    private static void RenderTextBlock(IContainer e, string title, string text)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(4).Text(title).FontSize(8).Bold().FontColor(AccentLight);
            col.Item().Text(text).FontSize(9).FontColor(TextPrimary);
        });
    }
}
