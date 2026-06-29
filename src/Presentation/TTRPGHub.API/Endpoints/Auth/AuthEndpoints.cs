using MediatR;
using TTRPGHub.Extensions;
using TTRPGHub.Features.Auth.Commands.ConfirmEmail;
using TTRPGHub.Features.Auth.Commands.ForgotPassword;
using TTRPGHub.Features.Auth.Commands.Login;
using TTRPGHub.Features.Auth.Commands.Register;
using TTRPGHub.Features.Auth.Commands.ResetPassword;

namespace TTRPGHub.Endpoints.Auth;

internal static class AuthEndpoints
{
    internal static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", async (RegisterCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToResponse();
        })
        .WithSummary("Регистрация нового пользователя")
        .AllowAnonymous();

        group.MapPost("/login", async (LoginCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToResponse();
        })
        .WithSummary("Вход в систему")
        .AllowAnonymous();

        group.MapPost("/confirm-email", async (ConfirmEmailCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToResponse();
        })
        .WithSummary("Подтверждение email")
        .AllowAnonymous();

        group.MapPost("/forgot-password", async (ForgotPasswordCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToResponse();
        })
        .WithSummary("Запрос сброса пароля")
        .AllowAnonymous();

        group.MapPost("/reset-password", async (ResetPasswordCommand command, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(command, ct);
            return result.ToResponse();
        })
        .WithSummary("Сброс пароля")
        .AllowAnonymous();
    }
}
