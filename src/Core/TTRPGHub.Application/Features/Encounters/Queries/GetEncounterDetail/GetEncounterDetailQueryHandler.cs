using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Common.Interfaces;
using TTRPGHub.Entities;
using TTRPGHub.Repositories;

namespace TTRPGHub.Features.Encounters.Queries.GetEncounterDetail;

internal sealed class GetEncounterDetailQueryHandler(
    IEncounterRepository repository,
    ICurrentUser currentUser
) : IRequestHandler<GetEncounterDetailQuery, Result<EncounterDetailDto>>
{
    public async Task<Result<EncounterDetailDto>> Handle(GetEncounterDetailQuery query, CancellationToken ct)
    {
        var encounter = await repository.GetByIdAsync(new EncounterId(query.EncounterId), ct);
        if (encounter is null) return Error.NotFound(nameof(Encounter));

        return Result<EncounterDetailDto>.Success(new EncounterDetailDto(
            encounter.Id.Value, encounter.CampaignId.Value, encounter.CreatedById.Value,
            encounter.Title, encounter.Description, encounter.Difficulty, encounter.Notes,
            encounter.Entries.Select(e => new EncounterEntryDto(e.Name, e.Count, e.Notes)).ToList(),
            encounter.CreatedById == currentUser.Id,
            encounter.CreatedAt, encounter.UpdatedAt));
    }
}
