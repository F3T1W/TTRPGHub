using MediatR;
using TTRPGHub.Common;

namespace TTRPGHub.Features.Auth.Commands.ResetPassword;

public sealed record ResetPasswordCommand(string Token, string NewPassword) : IRequest<Result>;
