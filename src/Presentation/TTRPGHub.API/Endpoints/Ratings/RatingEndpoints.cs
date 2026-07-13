using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Ratings.Commands.DeleteRating;
using TTRPGHub.Features.Ratings.Commands.DeleteSessionReview;
using TTRPGHub.Features.Ratings.Commands.RateSessionParticipant;
using TTRPGHub.Features.Ratings.Commands.RateUser;
using TTRPGHub.Features.Ratings.Queries.GetSessionReviews;
using TTRPGHub.Features.Ratings.Queries.GetUserRatings;
using TTRPGHub.Features.Ratings.Queries.GetUserSessionReviews;

namespace TTRPGHub.API.Endpoints.Ratings;

public static class RatingEndpoints
{
    public static IEndpointRouteBuilder MapRatings(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/ratings").WithTags("Ratings");

        g.MapGet("/{userId:guid}", async (Guid userId, IMediator m, CancellationToken ct) =>
            (await m.Send(new GetUserRatingsQuery(userId), ct)).ToResponse())
            .AllowAnonymous();

        g.MapPost("/{userId:guid}", async (Guid userId, RateUserRequest req, IMediator m, CancellationToken ct) =>
        {
            var result = await m.Send(new RateUserCommand(userId, req.Score, req.Comment, req.Role), ct);
            return result.IsSuccess
                ? Results.Created($"/api/v1/ratings/{result.Value}", result.Value)
                : result.ToResponse();
        }).RequireAuthorization();

        g.MapDelete("/{ratingId:guid}", async (Guid ratingId, IMediator m, CancellationToken ct) =>
            (await m.Send(new DeleteRatingCommand(ratingId), ct)).ToResponse())
            .RequireAuthorization();

        g.MapGet("/session-reviews/user/{userId:guid}", async (Guid userId, IMediator m, CancellationToken ct) =>
            (await m.Send(new GetUserSessionReviewsQuery(userId), ct)).ToResponse())
            .AllowAnonymous();

        g.MapDelete("/session-reviews/{reviewId:guid}", async (Guid reviewId, IMediator m, CancellationToken ct) =>
            (await m.Send(new DeleteSessionReviewCommand(reviewId), ct)).ToResponse())
            .RequireAuthorization();

        return app;
    }
}

public sealed record RateUserRequest(int Score, string? Comment, string Role);
