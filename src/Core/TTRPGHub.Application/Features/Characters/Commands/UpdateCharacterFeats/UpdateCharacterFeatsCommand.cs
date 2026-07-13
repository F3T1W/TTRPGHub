using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.UpdateCharacterFeats;

public sealed record UpdateCharacterFeatsCommand(Guid CharacterId, string SelectedFeatsJson) : IRequest<Result>;
