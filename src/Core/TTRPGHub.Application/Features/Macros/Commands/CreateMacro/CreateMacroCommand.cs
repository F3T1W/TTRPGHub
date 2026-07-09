using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Features.Macros.Shared;

namespace TTRPGHub.Features.Macros.Commands.CreateMacro;

public sealed record CreateMacroCommand(
    string Name, string? ImageUrl, string Type, string Command) : IRequest<Result<MacroDto>>;
