using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Pf2e;
using TTRPGHub.Repositories.Pf2e;

namespace TTRPGHub.Features.Pf2e.Hazards.Queries.GetHazardDetail;

internal sealed class GetPf2eHazardDetailQueryHandler(IPf2eHazardRepository repository)
    : IRequestHandler<GetPf2eHazardDetailQuery, Result<Pf2eHazardDetailDto>>
{
    public async Task<Result<Pf2eHazardDetailDto>> Handle(
        GetPf2eHazardDetailQuery query, CancellationToken ct)
    {
        var h = await repository.GetByIdAsync(new Pf2eHazardId(query.Id), ct);
        if (h is null) return Error.NotFound(nameof(Pf2eHazard));

        return new Pf2eHazardDetailDto(
            h.Id.Value, h.Slug, h.Name, h.NameRu, h.Level, h.Traits,
            h.StealthDc, h.StealthNote, h.Description, h.DisableText,
            h.ArmorClass, h.Fortitude, h.Reflex, h.Hardness, h.HitPoints,
            h.Immunities, h.AbilitiesText, h.ResetText, h.Source);
    }
}
