using MediatR;
using TTRPGHub.Application.Common.Interfaces;
using TTRPGHub.Domain.Common;
using TTRPGHub.Domain.Entities;
using TTRPGHub.Domain.Repositories;

namespace TTRPGHub.Application.Features.Characters.Commands.CreateCharacter;

internal sealed class CreateCharacterCommandHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork
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

        var c = characterResult.Value!;
        return new CreateCharacterResponse(c.Id.Value, c.Name, c.Race, c.Class, c.Level);
    }
}
