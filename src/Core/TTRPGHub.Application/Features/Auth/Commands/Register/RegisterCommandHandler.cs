using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Interfaces;
using TTRPGHub.Repositories;
using TTRPGHub.ValueObjects;

namespace TTRPGHub.Features.Auth.Commands.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IEmailConfirmationTokenRepository tokenRepo,
    IEmailService emailService,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    public async Task<Result<RegisterResponse>> Handle(RegisterCommand command, CancellationToken ct)
    {
        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return emailResult.Error!;

        var exists = await userRepository.ExistsByEmailAsync(emailResult.Value!.Value, ct);
        if (exists)
            return Error.Conflict("User");

        var passwordHash = passwordHasher.Hash(command.Password);
        var user = User.Create(command.Username, emailResult.Value!, passwordHash);

        await userRepository.AddAsync(user, ct);

        var token = EmailConfirmationToken.Create(user.Id);
        await tokenRepo.AddAsync(token, ct);

        await unitOfWork.SaveChangesAsync(ct);

        var confirmUrl = $"/confirm-email?token={token.Token}";
        await emailService.SendEmailConfirmationAsync(user.Email.Value, user.Username, confirmUrl, ct);

        return new RegisterResponse(user.Id.Value, user.Username, user.Email.Value);
    }
}
