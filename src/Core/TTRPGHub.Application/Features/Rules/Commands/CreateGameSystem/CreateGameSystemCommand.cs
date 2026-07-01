using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Rules.Commands.CreateGameSystem;

public sealed record CreateGameSystemCommand(string Name) : IRequest<Result<CreateGameSystemResponse>>;

public sealed record CreateGameSystemResponse(Guid Id, string Slug, string Name);
