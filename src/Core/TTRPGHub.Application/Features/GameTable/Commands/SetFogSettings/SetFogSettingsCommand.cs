using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetFogSettings;

public sealed record SetFogSettingsCommand(Guid SessionId, bool Enabled, int VisionRadiusFeet) : IRequest<Result>;
