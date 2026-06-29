using MediatR;
using TTRPGHub.Features.Forum.Commands.CreatePost;
using TTRPGHub.Features.Forum.Commands.CreateTopic;
using TTRPGHub.Features.Forum.Commands.ToggleLike;
using TTRPGHub.Features.Forum.Queries.GetCategories;
using TTRPGHub.Features.Forum.Queries.GetPosts;
using TTRPGHub.Features.Forum.Queries.GetTopics;
using TTRPGHub.Extensions;

namespace TTRPGHub.API.Endpoints.Forum;

public static class ForumEndpoints
{
    public static IEndpointRouteBuilder MapForum(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/forum").WithTags("Forum");

        g.MapGet("/categories", async (IMediator m, CancellationToken ct) =>
            (await m.Send(new GetCategoriesQuery(), ct)).ToResponse())
            .AllowAnonymous();

        g.MapGet("/categories/{slug}/topics", async (
            string slug, int page, int pageSize, IMediator m, CancellationToken ct) =>
            (await m.Send(new GetTopicsQuery(slug, page, pageSize), ct)).ToResponse())
            .AllowAnonymous();

        g.MapGet("/topics/{topicId:guid}/posts", async (
            Guid topicId, int page, int pageSize, IMediator m, CancellationToken ct) =>
            (await m.Send(new GetPostsQuery(topicId, page, pageSize), ct)).ToResponse())
            .AllowAnonymous();

        g.MapPost("/topics", async (CreateTopicCommand cmd, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(cmd, ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/forum/topics/{result.Value}", result.Value)
                : result.ToResponse();
        }).RequireAuthorization();

        g.MapPost("/topics/{topicId:guid}/posts", async (
            Guid topicId, CreatePostRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new CreatePostCommand(topicId, req.Content), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/forum/topics/{topicId}/posts/{result.Value}", result.Value)
                : result.ToResponse();
        }).RequireAuthorization();

        g.MapPost("/posts/{postId:guid}/like", async (
            Guid postId, IMediator m, CancellationToken ct) =>
            (await m.Send(new ToggleLikeCommand(postId), ct)).ToResponse())
            .RequireAuthorization();

        return app;
    }
}

public sealed record CreatePostRequest(string Content);
