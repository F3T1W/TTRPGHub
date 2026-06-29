using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Users.Queries.GetUserProfile;

namespace TTRPGHub.Endpoints.Users;

internal static class UsersEndpoints
{
    internal static void MapUsers(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users").WithTags("Users");

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetUserProfileQuery(id), ct);
            return result.ToResponse();
        })
        .WithSummary("Публичный профиль пользователя")
        .AllowAnonymous();
    }
}
