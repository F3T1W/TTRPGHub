using NSubstitute;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Entities.Forum;
using TTRPGHub.Features.Forum.Queries.GetCategories;
using TTRPGHub.Features.Forum.Queries.GetPosts;
using TTRPGHub.Features.Forum.Queries.GetTopics;
using TTRPGHub.Features.Rules.Queries.GetGameSystems;
using TTRPGHub.Features.Rules.Queries.GetRuleEntries;
using TTRPGHub.Features.Rules.Queries.GetRuleEntriesBySlugs;
using TTRPGHub.Features.Rules.Queries.GetRuleEntryDetail;
using TTRPGHub.Repositories;
using TTRPGHub.Repositories.Forum;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Application.Tests;

public class GetCategoriesQueryHandlerTests
{
    private readonly IForumCategoryRepository _categories = Substitute.For<IForumCategoryRepository>();

    private GetCategoriesQueryHandler CreateHandler() => new(_categories);

    [Fact]
    public async Task Handle_OrdersByDisplayOrder()
    {
        var second = ForumCategory.Create("Off-topic", "Random stuff", "off-topic", 2);
        var first = ForumCategory.Create("General", "General discussion", "general", 1);
        _categories.GetAllAsync().Returns([second, first]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetCategoriesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("General", result.Value![0].Name);
        Assert.Equal("Off-topic", result.Value[1].Name);
    }
}

public class GetTopicsQueryHandlerTests
{
    private readonly IForumCategoryRepository _categories = Substitute.For<IForumCategoryRepository>();
    private readonly IForumTopicRepository _topics = Substitute.For<IForumTopicRepository>();

    private GetTopicsQueryHandler CreateHandler() => new(_categories, _topics);

    [Fact]
    public async Task Handle_UnknownCategorySlug_ReturnsNotFound()
    {
        _categories.GetBySlugAsync(Arg.Any<string>()).Returns((ForumCategory?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTopicsQuery("missing"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ReturnsTopicsForCategory()
    {
        var category = ForumCategory.Create("General", "General discussion", "general");
        _categories.GetBySlugAsync("general").Returns(category);
        var topic = ForumTopic.Create(category.Id, UserId.New(), "Looking for a group");
        typeof(ForumTopic).GetProperty("Author")!.SetValue(topic, User.Create("op", Email.Create("op@test.com").Value!, "hash"));
        _topics.GetByCategoryAsync(category.Id, 1, 20).Returns((new List<ForumTopic> { topic }, 1));
        var handler = CreateHandler();

        var result = await handler.Handle(new GetTopicsQuery("general"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("op", result.Value.Items[0].AuthorUsername);
    }
}

public class GetPostsQueryHandlerTests
{
    private readonly IForumTopicRepository _topics = Substitute.For<IForumTopicRepository>();
    private readonly IForumPostRepository _posts = Substitute.For<IForumPostRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetPostsQueryHandler CreateHandler() => new(_topics, _posts, _currentUser);

    [Fact]
    public async Task Handle_TopicNotFound_ReturnsNotFound()
    {
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns((ForumTopic?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPostsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_LikedByMeIsFalse()
    {
        var category = ForumCategory.Create("General", "General discussion", "general");
        var topic = ForumTopic.Create(category.Id, UserId.New(), "Looking for a group");
        typeof(ForumTopic).GetProperty("Category")!.SetValue(topic, category);
        _topics.GetByIdAsync(Arg.Any<ForumTopicId>()).Returns(topic);
        var post = ForumPost.Create(topic.Id, UserId.New(), "First reply");
        typeof(ForumPost).GetProperty("Author")!.SetValue(post, User.Create("replier", Email.Create("replier@test.com").Value!, "hash"));
        _posts.GetByTopicAsync(topic.Id, 1, 20, Arg.Any<UserId?>()).Returns((new List<ForumPost> { post }, 1));
        _currentUser.IsAuthenticated.Returns(false);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetPostsQuery(topic.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.Posts.Items[0].LikedByMe);
        Assert.Equal("general", result.Value.CategorySlug);
    }
}

public class GetGameSystemsQueryHandlerTests
{
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetGameSystemsQueryHandler CreateHandler() => new(_systems, _currentUser);

    [Fact]
    public async Task Handle_MarksIsMineForSystemsOwnedByCurrentUser()
    {
        var ownerId = UserId.New();
        var mine = GameSystem.CreateCustom("my-system", "My System", ownerId);
        var official = GameSystem.CreateOfficial("pf2e", "Pathfinder 2e");
        _systems.GetAllAsync().Returns([mine, official]);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetGameSystemsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Single(s => s.Slug == "my-system").IsMine);
        Assert.False(result.Value!.Single(s => s.Slug == "pf2e").IsMine);
    }
}

public class GetRuleEntriesQueryHandlerTests
{
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entries = Substitute.For<IRuleEntryRepository>();

    private GetRuleEntriesQueryHandler CreateHandler() => new(_systems, _entries);

    [Fact]
    public async Task Handle_SystemNotFound_ReturnsNotFound()
    {
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns((GameSystem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetRuleEntriesQuery("missing", RuleCategory.Class, null), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ClampsPageSizeTo200()
    {
        var system = GameSystem.CreateOfficial("pf2e", "Pathfinder 2e");
        _systems.GetBySlugAsync("pf2e").Returns(system);
        _entries.SearchAsync(system.Id, RuleCategory.Class, null, 1, 200).Returns((IReadOnlyList<RuleEntry>)[]);
        _entries.CountAsync(system.Id, RuleCategory.Class, null).Returns(0);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetRuleEntriesQuery("pf2e", RuleCategory.Class, null, 1, 9999), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Value!.PageSize);
    }
}

public class GetRuleEntryDetailQueryHandlerTests
{
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entries = Substitute.For<IRuleEntryRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

    private GetRuleEntryDetailQueryHandler CreateHandler() => new(_systems, _entries, _currentUser);

    [Fact]
    public async Task Handle_EntryNotFound_ReturnsNotFound()
    {
        var system = GameSystem.CreateOfficial("pf2e", "Pathfinder 2e");
        _systems.GetBySlugAsync("pf2e").Returns(system);
        _entries.GetBySlugAsync(Arg.Any<GameSystemId>(), Arg.Any<RuleCategory>(), Arg.Any<string>()).Returns((RuleEntry?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetRuleEntryDetailQuery("pf2e", RuleCategory.Class, "fighter"), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_OfficialSystem_CanEditIsFalseEvenForCreator()
    {
        var system = GameSystem.CreateOfficial("pf2e", "Pathfinder 2e");
        _systems.GetBySlugAsync("pf2e").Returns(system);
        var entry = RuleEntry.Create(system.Id, RuleCategory.Class, "fighter", "Fighter", null, null, "{}", [], false, "SRD");
        _entries.GetBySlugAsync(system.Id, RuleCategory.Class, "fighter").Returns(entry);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetRuleEntryDetailQuery("pf2e", RuleCategory.Class, "fighter"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.CanEdit);
    }

    [Fact]
    public async Task Handle_CustomSystemOwner_CanEditIsTrue()
    {
        var ownerId = UserId.New();
        var system = GameSystem.CreateCustom("my-system", "My System", ownerId);
        _systems.GetBySlugAsync("my-system").Returns(system);
        var entry = RuleEntry.Create(system.Id, RuleCategory.Class, "gunslinger", "Gunslinger", null, null, "{}", [], true, "Homebrew");
        _entries.GetBySlugAsync(system.Id, RuleCategory.Class, "gunslinger").Returns(entry);
        _currentUser.Id.Returns(ownerId);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetRuleEntryDetailQuery("my-system", RuleCategory.Class, "gunslinger"), CancellationToken.None);

        Assert.True(result.Value!.CanEdit);
    }
}

public class GetRuleEntriesBySlugsQueryHandlerTests
{
    private readonly IGameSystemRepository _systems = Substitute.For<IGameSystemRepository>();
    private readonly IRuleEntryRepository _entries = Substitute.For<IRuleEntryRepository>();

    private GetRuleEntriesBySlugsQueryHandler CreateHandler() => new(_systems, _entries);

    [Fact]
    public async Task Handle_SystemNotFound_ReturnsNotFound()
    {
        _systems.GetBySlugAsync(Arg.Any<string>()).Returns((GameSystem?)null);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetRuleEntriesBySlugsQuery("missing", RuleCategory.Class, ["fighter"]), CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_ReturnsMatchedEntries()
    {
        var system = GameSystem.CreateOfficial("pf2e", "Pathfinder 2e");
        _systems.GetBySlugAsync("pf2e").Returns(system);
        var entry = RuleEntry.Create(system.Id, RuleCategory.Class, "fighter", "Fighter", null, null, "{}", [], false, "SRD");
        _entries.GetBySlugsAsync(system.Id, RuleCategory.Class, Arg.Any<IReadOnlyCollection<string>>()).Returns((IReadOnlyList<RuleEntry>)[entry]);
        var handler = CreateHandler();

        var result = await handler.Handle(new GetRuleEntriesBySlugsQuery("pf2e", RuleCategory.Class, ["fighter"]), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}
