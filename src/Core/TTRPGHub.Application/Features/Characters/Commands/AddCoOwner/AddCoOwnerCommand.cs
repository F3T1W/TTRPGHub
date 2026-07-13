using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.AddCoOwner;

public sealed record AddCoOwnerCommand(Guid CharacterId, string Username) : IRequest<Result>;
