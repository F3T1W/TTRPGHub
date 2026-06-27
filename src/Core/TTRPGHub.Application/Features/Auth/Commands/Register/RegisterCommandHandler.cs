using MediatR;
using TTRPGHub.Application.Common.Interfaces;
using TTRPGHub.Domain.Common;
using TTRPGHub.Domain.Entities;
using TTRPGHub.Domain.Repositories;
using TTRPGHub.Domain.ValueObjects;

namespace TTRPGHub.Application.Features.Auth.Commands.Register;

internal sealed class RegisterCommandHandler(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork
) : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
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
        await unitOfWork.SaveChangesAsync(ct);

        return new RegisterResponse(user.Id.Value, user.Username, user.Email.Value);
    }
}
