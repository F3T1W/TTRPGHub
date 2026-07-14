using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Macros.Commands.UnshareMacro;

public sealed record UnshareMacroCommand(Guid SessionId, Guid MacroId) : IRequest<Result>;
