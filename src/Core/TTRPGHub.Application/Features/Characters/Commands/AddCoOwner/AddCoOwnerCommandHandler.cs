using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.AddCoOwner;

internal sealed class AddCoOwnerCommandHandler(
    ICharacterRepository characterRepository,
    IUserRepository userRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<AddCoOwnerCommand, Result>
{
    public async Task<Result> Handle(AddCoOwnerCommand command, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(command.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        var (users, _) = await userRepository.SearchAsync(command.Username, 1, 5, ct);
        var target = users.FirstOrDefault(u => string.Equals(u.Username, command.Username, StringComparison.OrdinalIgnoreCase));
        if (target is null)
            return Error.NotFound(nameof(User));

        var result = character.AddCoOwner(target.Id.Value);
        if (result.IsFailure)
            return result;

        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync($"characters:owner:{currentUser.Id}", ct);
        await cache.RemoveAsync($"characters:owner:{target.Id}", ct);
        await cache.RemoveAsync($"characters:{command.CharacterId}", ct);

        return Result.Success();
    }
}
