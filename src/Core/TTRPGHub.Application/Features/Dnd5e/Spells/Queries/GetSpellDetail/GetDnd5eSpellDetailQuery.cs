using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpellDetail;

public sealed record GetDnd5eSpellDetailQuery(Guid Id) : IRequest<Result<SpellDetailDto>>;

public sealed record SpellDetailDto(
    Guid Id,
    string Name,
    int Level,
    string School,
    string CastingTime,
    string Range,
    string Components,
    string? Material,
    string Duration,
    bool Concentration,
    bool Ritual,
    string Description,
    string? HigherLevel,
    string Classes,
    string Source);
