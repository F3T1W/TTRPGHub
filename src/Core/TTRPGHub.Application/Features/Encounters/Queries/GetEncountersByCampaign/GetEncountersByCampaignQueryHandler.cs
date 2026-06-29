using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Encounters.Queries.GetEncountersByCampaign;

internal sealed class GetEncountersByCampaignQueryHandler(
    IEncounterRepository repository
) : IRequestHandler<GetEncountersByCampaignQuery, Result<IReadOnlyList<EncounterSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<EncounterSummaryDto>>> Handle(
        GetEncountersByCampaignQuery query, CancellationToken ct)
    {
        var encounters = await repository.GetByCampaignAsync(new CampaignId(query.CampaignId), ct);

        IReadOnlyList<EncounterSummaryDto> result = encounters
            .Select(e => new EncounterSummaryDto(
                e.Id.Value, e.CampaignId.Value, e.Title, e.Description,
                e.Difficulty, e.Entries.Count, e.UpdatedAt))
            .ToList();

        return Result<IReadOnlyList<EncounterSummaryDto>>.Success(result);
    }
}
