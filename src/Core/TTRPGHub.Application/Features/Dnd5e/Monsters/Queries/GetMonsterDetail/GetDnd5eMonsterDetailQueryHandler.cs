using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Repositories.Dnd5e;

namespace TTRPGHub.Features.Dnd5e.Monsters.Queries.GetMonsterDetail;

internal sealed class GetDnd5eMonsterDetailQueryHandler(IDnd5eMonsterRepository repository)
    : IRequestHandler<GetDnd5eMonsterDetailQuery, Result<MonsterDetailDto>>
{
    public async Task<Result<MonsterDetailDto>> Handle(
        GetDnd5eMonsterDetailQuery query, CancellationToken ct)
    {
        var m = await repository.GetByIdAsync(new Dnd5eMonsterId(query.Id), ct);
        if (m is null) return Error.NotFound(nameof(Dnd5eMonster));

        return new MonsterDetailDto(
            m.Id.Value, m.Name, m.Size, m.Type, m.Subtype, m.Alignment,
            m.ArmorClass, m.ArmorDesc, m.HitPoints, m.HitDice, m.Speed,
            m.Strength, m.Dexterity, m.Constitution,
            m.Intelligence, m.Wisdom, m.Charisma,
            m.ChallengeRating, m.Xp,
            m.SenseStr, m.LanguagesStr,
            m.Actions, m.SpecialAbilities, m.Reactions, m.LegendaryActions,
            m.Source);
    }
}
