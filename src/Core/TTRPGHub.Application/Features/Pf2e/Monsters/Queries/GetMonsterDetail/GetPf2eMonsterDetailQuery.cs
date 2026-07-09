using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Pf2e.Monsters.Queries.GetMonsterDetail;

public sealed record GetPf2eMonsterDetailQuery(Guid Id) : IRequest<Result<Pf2eMonsterDetailDto>>;

public sealed record Pf2eMonsterDetailDto(
    Guid Id,
    string Slug,
    string Name,
    int Level,
    string Size,
    string Traits,
    int Perception,
    string? Senses,
    string? Languages,
    string? Skills,
    int Strength, int Dexterity, int Constitution,
    int Intelligence, int Wisdom, int Charisma,
    int ArmorClass,
    int Fortitude, int Reflex, int Will,
    int HitPoints,
    string Speed,
    string? Attacks,
    string? Abilities,
    string Source,
    string? AttacksJson,
    string? ResistancesJson,
    string? WeaknessesJson,
    string? ImmunitiesJson,
    string? AurasJson);
