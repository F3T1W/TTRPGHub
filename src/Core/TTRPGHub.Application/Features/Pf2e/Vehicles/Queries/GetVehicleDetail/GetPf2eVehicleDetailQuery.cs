using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Pf2e.Vehicles.Queries.GetVehicleDetail;

public sealed record GetPf2eVehicleDetailQuery(Guid Id) : IRequest<Result<Pf2eVehicleDetailDto>>;

public sealed record Pf2eVehicleDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string NameRu,
    int Level,
    string? Size,
    string? Price,
    string? Dimensions,
    string? Crew,
    string? Passengers,
    string? PilotingCheck,
    int? ArmorClass,
    int? Fortitude,
    int? Hardness,
    int? HitPoints,
    int? BrokenThreshold,
    string? Immunities,
    string? Speed,
    string? Collision,
    string? AbilitiesText,
    string Source);
