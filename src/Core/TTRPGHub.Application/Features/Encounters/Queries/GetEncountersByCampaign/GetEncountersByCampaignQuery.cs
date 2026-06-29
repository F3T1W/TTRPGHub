using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Encounters.Queries.GetEncountersByCampaign;

public sealed record GetEncountersByCampaignQuery(Guid CampaignId)
    : IRequest<Result<IReadOnlyList<EncounterSummaryDto>>>;

public sealed record EncounterSummaryDto(
    Guid Id, Guid CampaignId, string Title, string? Description,
    EncounterDifficulty Difficulty, int EntryCount, DateTime UpdatedAt);
