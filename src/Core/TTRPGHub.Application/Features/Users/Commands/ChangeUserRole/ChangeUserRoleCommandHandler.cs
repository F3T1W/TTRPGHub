using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Users.Commands.ChangeUserRole;

internal sealed class ChangeUserRoleCommandHandler(
    IUserRepository users,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
) : IRequestHandler<ChangeUserRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role))
            return Error.Validation("Role", "Допустимые роли: Player, Moderator, Admin.");

        if (request.UserId == currentUser.Id.Value)
            return Error.Validation("Role", "Нельзя изменить собственную роль.");

        var user = await users.GetByIdAsync(new UserId(request.UserId), ct);
        if (user is null)
            return Error.NotFound(nameof(User));

        user.SetRole(role);
        users.Update(user);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
