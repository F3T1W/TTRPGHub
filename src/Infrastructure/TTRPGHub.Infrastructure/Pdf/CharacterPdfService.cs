using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;

namespace TTRPGHub.Infrastructure.Pdf;

internal sealed class CharacterPdfService : ICharacterPdfService
{
    private const string Accent = "#7c3aed";
    private const string AccentLight = "#a78bfa";
    private const string Gold = "#f59e0b";
    private const string Bg = "#0d0d1a";
    private const string Surface = "#13132a";
    private const string TextPrimary = "#ffffff";
    private const string TextMuted = "#9ca3af";
    private const string Border = "#2d2d4e";

    public byte[] Generate(Character c, byte[]? avatarBytes = null)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(20);
                page.PageColor(Bg);
                page.DefaultTextStyle(t => t.FontFamily("Arial").FontColor(TextPrimary).FontSize(9));

                page.Content().Column(col =>
                {
                    col.Spacing(8);
                    col.Item().Element(e => RenderHeader(e, c, avatarBytes));
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(2).Element(e => RenderAbilityScores(e, c));
                        row.ConstantItem(8);
                        row.RelativeItem(2).Element(e => RenderCombat(e, c));
                        row.ConstantItem(8);
                        row.RelativeItem(3).Element(e => RenderSavesAndSkills(e, c));
                    });
                    col.Item().Row(row =>
                    {
                        row.RelativeItem(3).Element(e => RenderBio(e, c));
                        row.ConstantItem(8);
                        row.RelativeItem(4).Element(e => RenderFeatures(e, c));
                        row.ConstantItem(8);
                        row.RelativeItem(3).Element(e => RenderEquipment(e, c));
                    });
                });
            });
        }).GeneratePdf();
    }

    private static void RenderHeader(IContainer e, Character c, byte[]? avatarBytes)
    {
        e.Background(Surface).Border(1).BorderColor(Accent).Padding(12).Row(row =>
        {
            if (avatarBytes is { Length: > 0 })
            {
                row.ConstantItem(70).PaddingRight(10)
                    .Width(60).Height(60)
                    .Image(avatarBytes).FitArea();
            }

            row.RelativeItem().Column(col =>
            {
                col.Item().Text(c.Name).FontSize(20).Bold().FontColor(AccentLight);
                col.Item().Text($"{c.Race}  ·  {c.Class}  ·  {c.Level} уровень")
                    .FontSize(11).FontColor(TextMuted);
                if (!string.IsNullOrEmpty(c.Background))
                    col.Item().Text($"Предыстория: {c.Background}").FontSize(9).FontColor(TextMuted);
                if (!string.IsNullOrEmpty(c.Alignment))
                    col.Item().Text($"Мировоззрение: {c.Alignment}").FontSize(9).FontColor(TextMuted);
            });

            row.ConstantItem(130).Column(col =>
            {
                col.Item().AlignRight().Text($"Бонус мастерства: +{c.ProficiencyBonus}")
                    .FontSize(10).FontColor(Gold);
                col.Item().AlignRight().Text($"Опыт: {c.ExperiencePoints} XP")
                    .FontSize(9).FontColor(TextMuted);
                col.Item().AlignRight().Text($"Инициатива: {ModStr(c.Initiative)}")
                    .FontSize(9).FontColor(TextMuted);
            });
        });
    }

    private static void RenderAbilityScores(IContainer e, Character c)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(6).Text("ХАРАКТЕРИСТИКИ").FontSize(8).Bold().FontColor(AccentLight);

            var stats = new[]
            {
                ("СИЛ", c.Strength, c.StrengthModifier),
                ("ЛОВ", c.Dexterity, c.DexterityModifier),
                ("ТЕЛ", c.Constitution, c.ConstitutionModifier),
                ("ИНТ", c.Intelligence, c.IntelligenceModifier),
                ("МДР", c.Wisdom, c.WisdomModifier),
                ("ХАР", c.Charisma, c.CharismaModifier),
            };

            foreach (var (name, score, mod) in stats)
            {
                col.Item().PaddingBottom(4).Border(1).BorderColor(Border).Padding(4).Column(inner =>
                {
                    inner.Item().AlignCenter().Text(name).FontSize(7).FontColor(TextMuted).Bold();
                    inner.Item().AlignCenter().Text(score.ToString()).FontSize(16).Bold().FontColor(TextPrimary);
                    inner.Item().AlignCenter().Text(ModStr(mod)).FontSize(11).FontColor(AccentLight);
                });
            }
        });
    }

    private static void RenderCombat(IContainer e, Character c)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(6).Text("БОЙ").FontSize(8).Bold().FontColor(AccentLight);

            StatBox(col, "КД", c.ArmorClass.ToString());
            StatBox(col, "ХП макс.", c.MaxHitPoints.ToString());
            StatBox(col, "ХП тек.", c.CurrentHitPoints.ToString());
            if (c.TemporaryHitPoints > 0)
                StatBox(col, "ХП врем.", c.TemporaryHitPoints.ToString());
            StatBox(col, "Скорость", $"{c.Speed} фут.");
            StatBox(col, "Кость хитов", c.HitDice);
            StatBox(col, "Пас. внимание", (10 + c.WisdomModifier).ToString());
        });
    }

    private static void StatBox(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(4).Row(row =>
        {
            row.RelativeItem().AlignMiddle().Text(label).FontSize(8).FontColor(TextMuted);
            row.ConstantItem(48).AlignRight().AlignMiddle()
                .Border(1).BorderColor(Border).Padding(3)
                .Text(value).FontSize(10).Bold().FontColor(TextPrimary);
        });
    }

    private static void RenderSavesAndSkills(IContainer e, Character c)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(4).Text("СПАСБРОСКИ").FontSize(8).Bold().FontColor(AccentLight);

            var saves = new[]
            {
                ("Сила", c.StrengthModifier, "Strength"),
                ("Ловкость", c.DexterityModifier, "Dexterity"),
                ("Телосложение", c.ConstitutionModifier, "Constitution"),
                ("Интеллект", c.IntelligenceModifier, "Intelligence"),
                ("Мудрость", c.WisdomModifier, "Wisdom"),
                ("Харизма", c.CharismaModifier, "Charisma"),
            };

            foreach (var (name, baseMod, key) in saves)
            {
                var prof = c.SavingThrowProficiencies.Contains(key);
                var total = baseMod + (prof ? c.ProficiencyBonus : 0);
                col.Item().PaddingBottom(2).Row(row =>
                {
                    row.ConstantItem(10).AlignMiddle().Text(prof ? "●" : "○")
                        .FontSize(8).FontColor(prof ? Gold : TextMuted);
                    row.ConstantItem(4);
                    row.RelativeItem().AlignMiddle().Text(name).FontSize(8).FontColor(TextMuted);
                    row.ConstantItem(24).AlignRight().AlignMiddle()
                        .Text(ModStr(total)).FontSize(9).Bold().FontColor(TextPrimary);
                });
            }

            col.Item().PaddingTop(8).PaddingBottom(4).Text("НАВЫКИ").FontSize(8).Bold().FontColor(AccentLight);

            var skills = new[]
            {
                ("Акробатика", c.DexterityModifier, "Acrobatics"),
                ("Анализ", c.IntelligenceModifier, "Investigation"),
                ("Атлетика", c.StrengthModifier, "Athletics"),
                ("Внимание", c.WisdomModifier, "Perception"),
                ("Выживание", c.WisdomModifier, "Survival"),
                ("Запугивание", c.CharismaModifier, "Intimidation"),
                ("История", c.IntelligenceModifier, "History"),
                ("Лечение", c.WisdomModifier, "Medicine"),
                ("Ловкость рук", c.DexterityModifier, "SleightOfHand"),
                ("Магия", c.IntelligenceModifier, "Arcana"),
                ("Маскировка", c.DexterityModifier, "Stealth"),
                ("Обман", c.CharismaModifier, "Deception"),
                ("Природа", c.IntelligenceModifier, "Nature"),
                ("Проницательность", c.WisdomModifier, "Insight"),
                ("Религия", c.IntelligenceModifier, "Religion"),
                ("Убеждение", c.CharismaModifier, "Persuasion"),
                ("Уход за животными", c.WisdomModifier, "AnimalHandling"),
            };

            foreach (var (name, baseMod, key) in skills)
            {
                var prof = c.SkillProficiencies.Contains(key);
                var total = baseMod + (prof ? c.ProficiencyBonus : 0);
                col.Item().PaddingBottom(2).Row(row =>
                {
                    row.ConstantItem(10).AlignMiddle().Text(prof ? "●" : "○")
                        .FontSize(7).FontColor(prof ? Gold : TextMuted);
                    row.ConstantItem(3);
                    row.RelativeItem().AlignMiddle().Text(name).FontSize(7.5f).FontColor(TextMuted);
                    row.ConstantItem(20).AlignRight().AlignMiddle()
                        .Text(ModStr(total)).FontSize(8).Bold().FontColor(TextPrimary);
                });
            }
        });
    }

    private static void RenderBio(IContainer e, Character c)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(6).Text("БИОГРАФИЯ").FontSize(8).Bold().FontColor(AccentLight);

            BioField(col, "Черты характера", c.PersonalityTraits);
            BioField(col, "Идеалы", c.Ideals);
            BioField(col, "Привязанности", c.Bonds);
            BioField(col, "Слабости", c.Flaws);
        });
    }

    private static void BioField(ColumnDescriptor col, string label, string? value)
    {
        col.Item().PaddingBottom(6).Column(inner =>
        {
            inner.Item().Text(label).FontSize(7).FontColor(AccentLight).Bold();
            inner.Item().PaddingTop(2).Border(1).BorderColor(Border).Padding(4)
                .Text(string.IsNullOrWhiteSpace(value) ? "—" : value)
                .FontColor(string.IsNullOrWhiteSpace(value) ? TextMuted : TextPrimary)
                .FontSize(8);
        });
    }

    private static void RenderFeatures(IContainer e, Character c)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(6).Text("УМЕНИЯ И ЧЕРТЫ").FontSize(8).Bold().FontColor(AccentLight);
            col.Item().Border(1).BorderColor(Border).Padding(6)
                .Text(string.IsNullOrWhiteSpace(c.FeaturesAndTraits) ? "—" : c.FeaturesAndTraits)
                .FontColor(string.IsNullOrWhiteSpace(c.FeaturesAndTraits) ? TextMuted : TextPrimary)
                .FontSize(8);
        });
    }

    private static void RenderEquipment(IContainer e, Character c)
    {
        e.Background(Surface).Border(1).BorderColor(Border).Padding(8).Column(col =>
        {
            col.Item().PaddingBottom(6).Text("СНАРЯЖЕНИЕ").FontSize(8).Bold().FontColor(AccentLight);
            col.Item().Border(1).BorderColor(Border).Padding(6)
                .Text(string.IsNullOrWhiteSpace(c.Equipment) ? "—" : c.Equipment)
                .FontColor(string.IsNullOrWhiteSpace(c.Equipment) ? TextMuted : TextPrimary)
                .FontSize(8);
        });
    }

    private static string ModStr(int mod) => mod >= 0 ? $"+{mod}" : mod.ToString();
}
