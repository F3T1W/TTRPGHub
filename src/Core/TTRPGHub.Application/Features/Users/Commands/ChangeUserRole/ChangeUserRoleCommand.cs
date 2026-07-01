using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Users.Commands.ChangeUserRole;

public sealed record ChangeUserRoleCommand(Guid UserId, string Role) : IRequest<Result>;
