using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.SetSceneEnvironment;

public sealed record SetSceneEnvironmentCommand(
    Guid SessionId,
    string? TerrainTagsJson,
    string AmbientLighting
) : IRequest<Result>;
