using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Auth.Commands.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : IRequest<Result>;
