using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities;
using TTRPGHub.Features.Encounters.Commands.CreateEncounter;

namespace TTRPGHub.Features.Encounters.Commands.UpdateEncounter;

public sealed record UpdateEncounterCommand(
    Guid EncounterId,
    string Title,
    string? Description,
    EncounterDifficulty Difficulty,
    string? Notes,
    List<EncounterEntryInput> Entries
) : IRequest<Result>;
