using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Pf2e.Spells.Queries.GetSpellDetail;

internal sealed class GetPf2eSpellDetailQueryHandler(IPf2eSpellRepository repository)
    : IRequestHandler<GetPf2eSpellDetailQuery, Result<Pf2eSpellDetailDto>>
{
    public async Task<Result<Pf2eSpellDetailDto>> Handle(
        GetPf2eSpellDetailQuery query, CancellationToken ct)
    {
        var spell = await repository.GetByIdAsync(new Pf2eSpellId(query.Id), ct);
        if (spell is null) return Error.NotFound(nameof(Pf2eSpell));

        return new Pf2eSpellDetailDto(
            spell.Id.Value, spell.Slug, spell.Name, spell.Level, spell.Traditions,
            spell.Traits, spell.Cast, spell.Range, spell.Area, spell.Targets,
            spell.Duration, spell.Description, spell.Heightened, spell.Source);
    }
}
