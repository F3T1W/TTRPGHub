using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Dnd5e;
using TTRPGHub.Repositories.Dnd5e;

namespace TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpellDetail;

internal sealed class GetDnd5eSpellDetailQueryHandler(IDnd5eSpellRepository repository)
    : IRequestHandler<GetDnd5eSpellDetailQuery, Result<SpellDetailDto>>
{
    public async Task<Result<SpellDetailDto>> Handle(
        GetDnd5eSpellDetailQuery query, CancellationToken ct)
    {
        var spell = await repository.GetByIdAsync(new Dnd5eSpellId(query.Id), ct);
        if (spell is null) return Error.NotFound(nameof(Dnd5eSpell));

        return new SpellDetailDto(
            spell.Id.Value, spell.Name, spell.Level, spell.School,
            spell.CastingTime, spell.Range, spell.Components, spell.Material,
            spell.Duration, spell.Concentration, spell.Ritual,
            spell.Description, spell.HigherLevel, spell.Classes, spell.Source);
    }
}
