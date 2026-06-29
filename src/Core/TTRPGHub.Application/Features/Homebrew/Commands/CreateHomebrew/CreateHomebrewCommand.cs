using MediatR;
using TTRPGHub.Common;
using TTRPGHub.Entities.Homebrew;

namespace TTRPGHub.Features.Homebrew.Commands.CreateHomebrew;

public sealed record CreateHomebrewCommand(
    string Title,
    string Description,
    string System,
    HomebrewType Type,
    string Content,
    string Tags)
    : IRequest<Result<Guid>>;
