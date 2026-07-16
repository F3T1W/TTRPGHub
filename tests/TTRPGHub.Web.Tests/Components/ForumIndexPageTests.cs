using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TTRPGHub.Services;
using ForumIndex = TTRPGHub.Pages.Forum.Index;

namespace TTRPGHub.Web.Tests.Components;

public class ForumIndexPageTests : BunitContext
{
    private readonly IApiClient _api = Substitute.For<IApiClient>();

    public ForumIndexPageTests()
    {
        Services.AddSingleton(_api);
    }

    [Fact]
    public void Render_WithCategories_ShowsNameDescriptionAndTopicCount()
    {
        _api.GetForumCategoriesAsync(Arg.Any<CancellationToken>()).Returns([
            new ForumCategoryDto(Guid.NewGuid(), "General Discussion", "Talk about anything", "general", 0, 42),
        ]);

        var cut = Render<ForumIndex>();

        Assert.Contains("General Discussion", cut.Markup);
        Assert.Contains("Talk about anything", cut.Markup);
        Assert.Contains("42 тем", cut.Markup);
    }

    [Fact]
    public void Render_CategoryLink_PointsToSlug()
    {
        _api.GetForumCategoriesAsync(Arg.Any<CancellationToken>()).Returns([
            new ForumCategoryDto(Guid.NewGuid(), "Rules Questions", "Ask about the rules", "rules-questions", 0, 5),
        ]);

        var cut = Render<ForumIndex>();

        var link = cut.Find("a[href='/forum/rules-questions']");
        Assert.Equal("Rules Questions", link.TextContent);
    }

    [Fact]
    public void Render_NoCategories_ShowsNoCards()
    {
        _api.GetForumCategoriesAsync(Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<ForumIndex>();

        Assert.Empty(cut.FindAll(".ta-card"));
    }

    [Fact]
    public void Render_MultipleCategories_ShowsAllOfThem()
    {
        _api.GetForumCategoriesAsync(Arg.Any<CancellationToken>()).Returns([
            new ForumCategoryDto(Guid.NewGuid(), "General Discussion", "Talk", "general", 0, 1),
            new ForumCategoryDto(Guid.NewGuid(), "Rules Questions", "Ask", "rules", 1, 2),
        ]);

        var cut = Render<ForumIndex>();

        Assert.Equal(2, cut.FindAll(".ta-card").Count);
    }
}
