using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.RemoveCoOwner;

public sealed record RemoveCoOwnerCommand(Guid CharacterId, Guid UserId) : IRequest<Result>;
