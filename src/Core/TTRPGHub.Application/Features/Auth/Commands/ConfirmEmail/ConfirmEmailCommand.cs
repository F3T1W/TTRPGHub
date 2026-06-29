using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Auth.Commands.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token) : IRequest<Result>;
