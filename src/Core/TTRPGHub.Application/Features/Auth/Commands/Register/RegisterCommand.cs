using MediatR;
using TTRPGHub.Domain.Common;

namespace TTRPGHub.Application.Features.Auth.Commands.Register;

public sealed record RegisterCommand(
    string Username,
    string Email,
    string Password
) : IRequest<Result<RegisterResponse>>;

public sealed record RegisterResponse(Guid UserId, string Username, string Email);
