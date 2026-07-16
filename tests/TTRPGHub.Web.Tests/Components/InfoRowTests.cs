using Bunit;
using TTRPGHub.Components.Shared;

namespace TTRPGHub.Web.Tests.Components;

public class InfoRowTests : BunitContext
{
    [Fact]
    public void Render_WithValue_ShowsLabelAndValue()
    {
        var cut = Render<InfoRow>(p => p
            .Add(c => c.Label, "Race")
            .Add(c => c.Value, "Human"));

        Assert.Contains("Race", cut.Markup);
        Assert.Contains("Human", cut.Markup);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Render_BlankValue_RendersNothing(string? value)
    {
        var cut = Render<InfoRow>(p => p
            .Add(c => c.Label, "Race")
            .Add(c => c.Value, value));

        Assert.Equal("", cut.Markup.Trim());
    }
}
