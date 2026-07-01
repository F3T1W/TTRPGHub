using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.ImportCharacter;

internal sealed class ImportCharacterCommandHandler(
    ICharacterRepository characterRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ICacheService cache
) : IRequestHandler<ImportCharacterCommand, Result<ImportCharacterResponse>>
{
    public async Task<Result<ImportCharacterResponse>> Handle(ImportCharacterCommand cmd, CancellationToken ct)
    {
        var createResult = Character.Create(currentUser.Id, cmd.Name, cmd.Race, cmd.Class, cmd.Level);
        if (createResult.IsFailure) return createResult.Error!;

        var character = createResult.Value!;

        var data = new UpdateSheetData(
            cmd.Name, cmd.Race, cmd.Class, cmd.Level, cmd.IsPublic,
            cmd.Background, cmd.Alignment, cmd.ExperiencePoints,
            cmd.PersonalityTraits, cmd.Ideals, cmd.Bonds, cmd.Flaws,
            cmd.Strength, cmd.Dexterity, cmd.Constitution,
            cmd.Intelligence, cmd.Wisdom, cmd.Charisma,
            cmd.MaxHitPoints, cmd.CurrentHitPoints, cmd.TemporaryHitPoints,
            cmd.ArmorClass, cmd.Speed, cmd.HitDice,
            cmd.SkillProficiencies ?? [],
            cmd.SavingThrowProficiencies ?? [],
            cmd.FeaturesAndTraits, cmd.Equipment);

        var updateResult = character.UpdateSheet(data);
        if (updateResult.IsFailure) return updateResult.Error!;

        await characterRepository.AddAsync(character, ct);
        await unitOfWork.SaveChangesAsync(ct);
        await cache.RemoveAsync($"characters:owner:{currentUser.Id}", ct);

        return new ImportCharacterResponse(character.Id.Value, character.Name);
    }
}
