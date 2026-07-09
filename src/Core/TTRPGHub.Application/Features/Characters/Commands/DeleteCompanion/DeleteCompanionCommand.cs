using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.DeleteCompanion;

public sealed record DeleteCompanionCommand(Guid CompanionId) : IRequest<Result>;
