using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Pf2e.Hazards.Queries.GetHazardDetail;

public sealed record GetPf2eHazardDetailQuery(Guid Id) : IRequest<Result<Pf2eHazardDetailDto>>;

public sealed record Pf2eHazardDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string NameRu,
    int Level,
    string Traits,
    int StealthDc,
    string? StealthNote,
    string? Description,
    string? DisableText,
    int? ArmorClass,
    int? Fortitude,
    int? Reflex,
    int? Hardness,
    int? HitPoints,
    string? Immunities,
    string? AbilitiesText,
    string? ResetText,
    string Source);
