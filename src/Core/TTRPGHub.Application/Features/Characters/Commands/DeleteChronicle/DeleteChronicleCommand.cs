using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Characters.Commands.DeleteChronicle;

public sealed record DeleteChronicleCommand(Guid ChronicleId) : IRequest<Result>;
