using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Interfaces;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Auth.Commands.ForgotPassword;

internal sealed class ForgotPasswordCommandHandler(
    IUserRepository userRepo,
    IPasswordResetTokenRepository tokenRepo,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await userRepo.GetByEmailAsync(request.Email, ct);
        // Намеренно возвращаем Success даже если пользователь не найден (защита от enumeration)
        if (user is null)
            return Result.Success();

        var token = PasswordResetToken.Create(user.Id);
        await tokenRepo.AddAsync(token, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var resetUrl = $"/reset-password?token={token.Token}";
        await emailService.SendPasswordResetAsync(user.Email.Value, user.Username, resetUrl, ct);

        return Result.Success();
    }
}
