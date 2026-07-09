using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Dnd5e.Spells.Queries.GetSpells;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Pf2e.Vehicles.Queries.GetVehicles;

internal sealed class GetPf2eVehiclesQueryHandler(IPf2eVehicleRepository repository)
    : IRequestHandler<GetPf2eVehiclesQuery, Result<PagedResult<Pf2eVehicleSummaryDto>>>
{
    public async Task<Result<PagedResult<Pf2eVehicleSummaryDto>>> Handle(
        GetPf2eVehiclesQuery query, CancellationToken ct)
    {
        var (items, total) = await repository.SearchAsync(
            query.Search, query.Level, query.Page, query.PageSize, ct);

        var dtos = items.Select(v => new Pf2eVehicleSummaryDto(
            v.Id.Value, v.Slug, v.Name, v.NameRu, v.Level, v.Size, v.ArmorClass, v.HitPoints)).ToList();

        return new PagedResult<Pf2eVehicleSummaryDto>(dtos, total, query.Page, query.PageSize);
    }
}
