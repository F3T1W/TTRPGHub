using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.ActivateScene;

public sealed record ActivateSceneCommand(Guid SessionId, Guid SceneId) : IRequest<Result>;
