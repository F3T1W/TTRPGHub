using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.CreateCharacter;

internal sealed class CreateCharacterCommandHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<CreateCharacterCommand, Result<CreateCharacterResponse>>
{
    public async Task<Result<CreateCharacterResponse>> Handle(CreateCharacterCommand command, CancellationToken ct)
    {
        var characterResult = Character.Create(
            currentUser.Id,
            command.Name,
            command.Race,
            command.Class,
            command.Level);

        if (characterResult.IsFailure)
            return characterResult.Error!;

        await characterRepository.AddAsync(characterResult.Value!, ct);
        await unitOfWork.SaveChangesAsync(ct);

        await cache.RemoveAsync($"characters:owner:{currentUser.Id}", ct);

        var c = characterResult.Value!;
        return new CreateCharacterResponse(c.Id.Value, c.Name, c.Race, c.Class, c.Level);
    }
}
