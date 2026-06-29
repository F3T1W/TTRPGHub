using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Encounters.Commands.DeleteEncounter;

public sealed record DeleteEncounterCommand(Guid EncounterId) : IRequest<Result>;
