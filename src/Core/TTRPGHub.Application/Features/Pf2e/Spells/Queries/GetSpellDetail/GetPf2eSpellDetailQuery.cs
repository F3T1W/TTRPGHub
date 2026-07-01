using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Pf2e.Spells.Queries.GetSpellDetail;

public sealed record GetPf2eSpellDetailQuery(Guid Id) : IRequest<Result<Pf2eSpellDetailDto>>;

public sealed record Pf2eSpellDetailDto(
    Guid Id,
    string Slug,
    string Name,
    int Level,
    string Traditions,
    string Traits,
    string Cast,
    string? Range,
    string? Area,
    string? Targets,
    string Duration,
    string Description,
    string? Heightened,
    string Source);
