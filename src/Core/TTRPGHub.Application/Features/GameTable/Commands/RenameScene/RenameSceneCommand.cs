using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.RenameScene;

public sealed record RenameSceneCommand(Guid SessionId, Guid SceneId, string Name) : IRequest<Result>;
