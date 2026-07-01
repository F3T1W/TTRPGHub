using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Users.Commands.ChangeUserRole;
using TTRPGHub.Features.Users.Queries.GetAllUsers;
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

        group.MapGet("/admin", async (string? search, int page, int pageSize, ISender sender, CancellationToken ct) =>
            (await sender.Send(new GetAllUsersQuery(search, page == 0 ? 1 : page, pageSize == 0 ? 30 : pageSize), ct)).ToResponse())
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .WithSummary("Список пользователей для управления ролями (только Admin)");

        group.MapPatch("/admin/{id:guid}/role", async (Guid id, ChangeRoleRequest req, ISender sender, CancellationToken ct) =>
            (await sender.Send(new ChangeUserRoleCommand(id, req.Role), ct)).ToResponse())
            .RequireAuthorization(p => p.RequireRole("Admin"))
            .WithSummary("Изменить роль пользователя (только Admin)");
    }
}

internal sealed record ChangeRoleRequest(string Role);
