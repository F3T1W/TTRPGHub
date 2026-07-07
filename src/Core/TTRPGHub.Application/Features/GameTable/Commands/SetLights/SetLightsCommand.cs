using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetLights;

public sealed record SetLightsCommand(Guid SessionId, string? LightsJson) : IRequest<Result>;
