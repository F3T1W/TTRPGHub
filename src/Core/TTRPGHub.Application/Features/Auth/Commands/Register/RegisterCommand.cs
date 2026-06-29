using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password
) : IRequest<Result<RegisterResponse>>;

public sealed record RegisterResponse(Guid UserId, string Username, string Email);
