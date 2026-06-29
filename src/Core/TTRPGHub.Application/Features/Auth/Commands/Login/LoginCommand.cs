using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password
) : IRequest<Result<LoginResponse>>;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string Username,
    Guid UserId
);
