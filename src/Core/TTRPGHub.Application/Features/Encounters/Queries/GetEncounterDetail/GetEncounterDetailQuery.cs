using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Encounters.Queries.GetEncounterDetail;

public sealed record GetEncounterDetailQuery(Guid EncounterId)
    : IRequest<Result<EncounterDetailDto>>;

public sealed record EncounterDetailDto(
    Guid Id, Guid CampaignId, Guid CreatedById,
    string Title, string? Description,
    EncounterDifficulty Difficulty, string? Notes,
    IReadOnlyList<EncounterEntryDto> Entries,
    bool IsCreator, DateTime CreatedAt, DateTime UpdatedAt);

public sealed record EncounterEntryDto(string Name, int Count, string? Notes);
