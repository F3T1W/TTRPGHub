using Bunit;
using Microsoft.AspNetCore.Components;
using TTRPGHub.Components.Shared;

namespace TTRPGHub.Web.Tests.Components;

public class PersonalityFieldTests : BunitContext
{
    [Fact]
    public void Render_ViewMode_WithValue_ShowsLabelAndValue()
    {
        var cut = Render<PersonalityField>(p => p
            .Add(c => c.Label, "Ideals")
            .Add(c => c.Value, "Honor above all")
            .Add(c => c.IsEditing, false));

        Assert.Contains("Ideals", cut.Markup);
        Assert.Contains("Honor above all", cut.Markup);
        Assert.Empty(cut.FindAll("textarea"));
    }

    [Fact]
    public void Render_ViewMode_BlankValue_RendersNothing()
    {
        var cut = Render<PersonalityField>(p => p
            .Add(c => c.Label, "Ideals")
            .Add(c => c.Value, (string?)null)
            .Add(c => c.IsEditing, false));

        Assert.Equal("", cut.Markup.Trim());
    }

    [Fact]
    public void Render_EditMode_ShowsTextareaWithEditValue()
    {
        var cut = Render<PersonalityField>(p => p
            .Add(c => c.Label, "Ideals")
            .Add(c => c.EditValue, "Draft text")
            .Add(c => c.IsEditing, true));

        var textarea = cut.Find("textarea");
        Assert.Equal("Draft text", textarea.GetAttribute("value"));
    }

    [Fact]
    public void EditMode_TypingInTextarea_InvokesOnChangeWithNewValue()
    {
        string? received = null;
        var cut = Render<PersonalityField>(p => p
            .Add(c => c.Label, "Ideals")
            .Add(c => c.IsEditing, true)
            .Add(c => c.OnChange, EventCallback.Factory.Create<string?>(this, v => received = v)));

        cut.Find("textarea").Input("New text");

        Assert.Equal("New text", received);
    }
}
