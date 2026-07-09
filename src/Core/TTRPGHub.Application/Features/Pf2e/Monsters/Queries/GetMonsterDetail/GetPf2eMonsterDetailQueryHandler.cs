using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Pf2e.Monsters.Queries.GetMonsterDetail;

internal sealed class GetPf2eMonsterDetailQueryHandler(IPf2eMonsterRepository repository)
    : IRequestHandler<GetPf2eMonsterDetailQuery, Result<Pf2eMonsterDetailDto>>
{
    public async Task<Result<Pf2eMonsterDetailDto>> Handle(
        GetPf2eMonsterDetailQuery query, CancellationToken ct)
    {
        var m = await repository.GetByIdAsync(new Pf2eMonsterId(query.Id), ct);
        if (m is null) return Error.NotFound(nameof(Pf2eMonster));

        return new Pf2eMonsterDetailDto(
            m.Id.Value, m.Slug, m.Name, m.Level, m.Size, m.Traits, m.Perception,
            m.Senses, m.Languages, m.Skills,
            m.Strength, m.Dexterity, m.Constitution,
            m.Intelligence, m.Wisdom, m.Charisma,
            m.ArmorClass, m.Fortitude, m.Reflex, m.Will, m.HitPoints,
            m.Speed, m.Attacks, m.Abilities, m.Source, m.AttacksJson,
            m.ResistancesJson, m.WeaknessesJson, m.ImmunitiesJson, m.AurasJson);
    }
}
