using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Macros.Commands.UpdateMacro;

public sealed record UpdateMacroCommand(
    Guid MacroId, string Name, string? ImageUrl, string Type, string Command) : IRequest<Result>;
