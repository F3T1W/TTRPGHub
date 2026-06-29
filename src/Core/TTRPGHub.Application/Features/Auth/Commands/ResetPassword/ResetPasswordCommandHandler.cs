using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Interfaces;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Auth.Commands.ResetPassword;

internal sealed class ResetPasswordCommandHandler(
    IPasswordResetTokenRepository tokenRepo,
    IUserRepository userRepo,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork) : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var token = await tokenRepo.GetByTokenAsync(request.Token, ct);
        if (token is null || !token.IsValid())
            return Error.Validation("Token", "Токен сброса пароля недействителен или истёк.");

        var user = await userRepo.GetByIdAsync(token.UserId, ct);
        if (user is null)
            return Error.NotFound("User");

        user.SetPassword(passwordHasher.Hash(request.NewPassword));
        userRepo.Update(user);

        token.MarkUsed();
        tokenRepo.Update(token);

        await unitOfWork.SaveChangesAsync(ct);
        return Result.Success();
    }
}
