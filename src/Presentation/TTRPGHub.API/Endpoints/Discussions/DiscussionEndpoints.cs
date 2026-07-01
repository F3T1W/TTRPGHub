using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Discussions.Commands.AddPost;
using TTRPGHub.Features.Discussions.Commands.DeletePost;
using TTRPGHub.Features.Discussions.Commands.ToggleLike;
using TTRPGHub.Features.Discussions.Queries.GetDiscussion;

namespace TTRPGHub.API.Endpoints.Discussions;

public static class DiscussionEndpoints
{
    public static void MapDiscussions(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/discussions").WithTags("Discussions");

        group.MapGet("/{entityType}/{entitySlug}", async (
            string entityType, string entitySlug, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetDiscussionQuery(entityType, entitySlug), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        }).AllowAnonymous();

        group.MapPost("/{entityType}/{entitySlug}", async (
            string entityType, string entitySlug, AddPostRequest req, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new AddPostCommand(entityType, entitySlug, req.Content, req.ParentId), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : result.ToResponse();
        }).RequireAuthorization();

        group.MapPost("/posts/{postId:guid}/like", async (
            Guid postId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ToggleLikeCommand(postId), ct);
            return result.IsSuccess ? Results.Ok(new { isLiked = result.Value }) : result.ToResponse();
        }).RequireAuthorization();

        group.MapDelete("/posts/{postId:guid}", async (
            Guid postId, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new DeletePostCommand(postId), ct);
            return result.IsSuccess ? Results.NoContent() : result.ToResponse();
        }).RequireAuthorization();
    }
}

public sealed record AddPostRequest(string Content, Guid? ParentId = null);
