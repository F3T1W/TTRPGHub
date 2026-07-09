using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;

namespace TTRPGHub.Features.Pf2e.Vehicles.Queries.GetVehicles;

public sealed record GetPf2eVehiclesQuery(
    string? Search,
    int? Level,
    int Page = 1,
    int PageSize = 30
) : IRequest<Result<PagedResult<Pf2eVehicleSummaryDto>>>;

public sealed record Pf2eVehicleSummaryDto(
    Guid Id,
    string Slug,
    string Name,
    string NameRu,
    int Level,
    string? Size,
    int? ArmorClass,
    int? HitPoints);
