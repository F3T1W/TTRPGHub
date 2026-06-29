using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Homebrew.Commands.DeleteHomebrew;

public sealed record DeleteHomebrewCommand(Guid Id) : IRequest<Result>;
