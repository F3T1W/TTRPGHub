using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Macros.Commands.DeleteMacro;

public sealed record DeleteMacroCommand(Guid MacroId) : IRequest<Result>;
