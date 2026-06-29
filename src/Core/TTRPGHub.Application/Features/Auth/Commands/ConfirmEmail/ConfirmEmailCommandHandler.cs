using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Auth.Commands.ConfirmEmail;

internal sealed class ConfirmEmailCommandHandler(
    IEmailConfirmationTokenRepository tokenRepo,
    IUnitOfWork unitOfWork) : IRequestHandler<ConfirmEmailCommand, Result>
{
    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        var token = await tokenRepo.GetByTokenAsync(request.Token, ct);
        if (token is null || !token.IsValid())
            return Error.Validation("Token", "Токен подтверждения недействителен или истёк.");

        token.MarkUsed();
        tokenRepo.Update(token);
        await unitOfWork.SaveChangesAsync(ct);

        return Result.Success();
    }
}
