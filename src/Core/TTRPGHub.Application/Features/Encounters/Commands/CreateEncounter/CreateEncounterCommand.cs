using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;

namespace TTRPGHub.Features.Encounters.Commands.CreateEncounter;

public sealed record CreateEncounterCommand(
    Guid CampaignId,
    string Title,
    string? Description,
    EncounterDifficulty Difficulty,
    string? Notes,
    List<EncounterEntryInput> Entries
) : IRequest<Result<CreateEncounterResponse>>;

public sealed record EncounterEntryInput(string Name, int Count, string? Notes);
public sealed record CreateEncounterResponse(Guid EncounterId);
