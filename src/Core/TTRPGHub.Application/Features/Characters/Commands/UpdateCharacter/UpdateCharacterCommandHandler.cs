using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Features.Characters.Shared;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Characters.Commands.UpdateCharacter;

internal sealed class UpdateCharacterCommandHandler(
    ICharacterRepository characterRepository,
    ITableTokenRepository tokenRepository,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ITableNotifier notifier,
    ICacheService cache
) : IRequestHandler<UpdateCharacterCommand, Result>
{
    public async Task<Result> Handle(UpdateCharacterCommand command, CancellationToken ct)
    {
        var character = await characterRepository.GetByIdAsync(new CharacterId(command.CharacterId), ct);
        if (character is null)
            return Error.NotFound(nameof(Character));

        if (character.OwnerId != currentUser.Id)
            return Error.Unauthorized();

        var data = new UpdateSheetData(
            command.Name, command.Race, command.Class, command.Level, command.IsPublic,
            command.Background, command.Alignment, command.ExperiencePoints,
            command.PersonalityTraits, command.Ideals, command.Bonds, command.Flaws,
            command.Strength, command.Dexterity, command.Constitution,
            command.Intelligence, command.Wisdom, command.Charisma,
            command.MaxHitPoints, command.CurrentHitPoints, command.TemporaryHitPoints,
            command.ArmorClass, command.Speed, command.HitDice,
            command.SkillProficiencies, command.SavingThrowProficiencies,
            command.FeaturesAndTraits, command.Equipment);

        var result = character.UpdateSheet(data);
        if (result.IsFailure) return result;

        var tokenUpdates = await CharacterTokenSync.SyncTokensAsync(character, tokenRepository, ct);

        await unitOfWork.SaveChangesAsync(ct);

        foreach (var (sessionId, dto) in tokenUpdates)
            await notifier.NotifyTokenUpdatedAsync(sessionId, dto, ct);

        await cache.RemoveAsync($"characters:owner:{currentUser.Id}", ct);
        await cache.RemoveAsync($"characters:{command.CharacterId}", ct);

        return Result.Success();
    }
}
