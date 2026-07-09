using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Pf2e.Vehicles.Queries.GetVehicleDetail;

internal sealed class GetPf2eVehicleDetailQueryHandler(IPf2eVehicleRepository repository)
    : IRequestHandler<GetPf2eVehicleDetailQuery, Result<Pf2eVehicleDetailDto>>
{
    public async Task<Result<Pf2eVehicleDetailDto>> Handle(
        GetPf2eVehicleDetailQuery query, CancellationToken ct)
    {
        var v = await repository.GetByIdAsync(new Pf2eVehicleId(query.Id), ct);
        if (v is null) return Error.NotFound(nameof(Pf2eVehicle));

        return new Pf2eVehicleDetailDto(
            v.Id.Value, v.Slug, v.Name, v.NameRu, v.Level, v.Size, v.Price,
            v.Dimensions, v.Crew, v.Passengers, v.PilotingCheck,
            v.ArmorClass, v.Fortitude, v.Hardness, v.HitPoints, v.BrokenThreshold,
            v.Immunities, v.Speed, v.Collision, v.AbilitiesText, v.Source);
    }
}
