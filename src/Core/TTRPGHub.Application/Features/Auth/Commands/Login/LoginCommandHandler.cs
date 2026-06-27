using MediatR;
using TTRPGHub.Application.Common.Interfaces;
using TTRPGHub.Domain.Common;
using TTRPGHub.Domain.Repositories;

namespace TTRPGHub.Application.Features.Auth.Commands.Login;

internal sealed class LoginCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IJwtService jwtService
) : IRequestHandler<LoginCommand, Result<LoginResponse>>
{
    public async Task<Result<LoginResponse>> Handle(LoginCommand command, CancellationToken ct)
    {
        var user = await userRepository.GetByEmailAsync(command.Email.Trim().ToLowerInvariant(), ct);
        if (user is null)
            return Error.Validation("Credentials", "Неверный email или пароль.");

        if (!passwordHasher.Verify(command.Password, user.PasswordHash))
            return Error.Validation("Credentials", "Неверный email или пароль.");

        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        return new LoginResponse(accessToken, refreshToken, user.Username);
    }
}
