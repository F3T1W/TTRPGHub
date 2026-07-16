using Bunit;
using GlossaryIndex = TTRPGHub.Pages.Glossary.Index;

namespace TTRPGHub.Web.Tests.Components;

public class GlossaryIndexPageTests : BunitContext
{
    [Fact]
    public void Render_Default_ShowsAllTerms()
    {
        var cut = Render<GlossaryIndex>();

        Assert.Contains("HP", cut.Markup);
        Assert.Contains("ДМ", cut.Markup);
        Assert.Contains("Ваншот", cut.Markup);
    }

    [Fact]
    public void Search_MatchesNameCaseInsensitively_FiltersOthersOut()
    {
        var cut = Render<GlossaryIndex>();

        cut.Find("input.form-control").Input("ваншот");

        Assert.Contains("Ваншот", cut.Markup);
        Assert.DoesNotContain("Мегадунжн", cut.Markup);
    }

    [Fact]
    public void Search_MatchesAltField_ShowsMatchingTerm()
    {
        var cut = Render<GlossaryIndex>();

        cut.Find("input.form-control").Input("one-shot");

        Assert.Contains("Ваншот", cut.Markup);
    }

    [Fact]
    public void CategoryFilter_Mechanics_ShowsOnlyMechanicsTerms()
    {
        var cut = Render<GlossaryIndex>();

        cut.Find("select.form-select").Change("Механика");

        Assert.Contains("Крит", cut.Markup);
        Assert.DoesNotContain("Ваншот", cut.Markup);
    }

    [Fact]
    public void LetterFilter_ClickingLetter_ShowsOnlyTermsStartingWithIt()
    {
        var cut = Render<GlossaryIndex>();

        var letterButton = cut.FindAll("button.btn-sm").Single(b => b.TextContent == "В");
        letterButton.Click();

        Assert.Contains("Ваншот", cut.Markup);
        Assert.DoesNotContain("ДМ", cut.Markup);
    }

    [Fact]
    public void Search_NoMatches_ShowsNotFoundMessage()
    {
        var cut = Render<GlossaryIndex>();

        cut.Find("input.form-control").Input("zzzznonexistentzzzz");

        Assert.Contains("Термины не найдены", cut.Markup);
    }
}
