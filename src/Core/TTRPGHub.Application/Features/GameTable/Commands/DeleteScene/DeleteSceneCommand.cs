using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.DeleteScene;

public sealed record DeleteSceneCommand(Guid SessionId, Guid SceneId) : IRequest<Result>;
