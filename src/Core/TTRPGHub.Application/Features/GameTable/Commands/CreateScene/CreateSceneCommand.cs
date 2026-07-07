using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.GameTable.Commands.CreateScene;

public sealed record CreateSceneCommand(Guid SessionId, string Name) : IRequest<Result<CreateSceneResponse>>;

public sealed record CreateSceneResponse(Guid Id, string Name);
