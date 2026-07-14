using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Macros.Commands.ShareMacro;

public sealed record ShareMacroCommand(Guid SessionId, Guid MacroId) : IRequest<Result>;
