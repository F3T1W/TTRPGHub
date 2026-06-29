using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TTRPGHub.Interfaces;

namespace TTRPGHub.Email;

internal sealed class SmtpEmailService(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly SmtpOptions _opts = options.Value;

    public async Task SendEmailConfirmationAsync(string toEmail, string username, string confirmUrl, CancellationToken ct = default)
    {
        var fullUrl = $"{_opts.AppBaseUrl}{confirmUrl}";
        var subject = "Подтвердите email — Таверна Аферистов";
        var body = $"""
            <html><body style="background:#0d0d1a;color:#e2e8f0;font-family:sans-serif;padding:32px;">
            <h2 style="color:#a78bfa;">Таверна Аферистов</h2>
            <p>Привет, <strong>{username}</strong>!</p>
            <p>Для завершения регистрации подтвердите ваш email:</p>
            <a href="{fullUrl}"
               style="display:inline-block;padding:12px 24px;background:#7c3aed;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold;margin:16px 0;">
               Подтвердить email
            </a>
            <p style="color:#94a3b8;font-size:12px;">Ссылка действительна 24 часа.</p>
            </body></html>
            """;
        await SendAsync(toEmail, subject, body, fullUrl, ct);
    }

    public async Task SendPasswordResetAsync(string toEmail, string username, string resetUrl, CancellationToken ct = default)
    {
        var fullUrl = $"{_opts.AppBaseUrl}{resetUrl}";
        var subject = "Сброс пароля — Таверна Аферистов";
        var body = $"""
            <html><body style="background:#0d0d1a;color:#e2e8f0;font-family:sans-serif;padding:32px;">
            <h2 style="color:#a78bfa;">Таверна Аферистов</h2>
            <p>Привет, <strong>{username}</strong>!</p>
            <p>Получен запрос на сброс пароля. Нажмите кнопку ниже:</p>
            <a href="{fullUrl}"
               style="display:inline-block;padding:12px 24px;background:#7c3aed;color:#fff;border-radius:6px;text-decoration:none;font-weight:bold;margin:16px 0;">
               Сбросить пароль
            </a>
            <p style="color:#94a3b8;font-size:12px;">Ссылка действительна 2 часа. Если вы не запрашивали сброс — проигнорируйте это письмо.</p>
            </body></html>
            """;
        await SendAsync(toEmail, subject, body, fullUrl, ct);
    }

    private async Task SendAsync(string to, string subject, string htmlBody, string actionUrl, CancellationToken ct)
    {
        if (!_opts.Enabled)
        {
            logger.LogInformation("[Email disabled] To={To} Subject={Subject} Url={Url}", to, subject, actionUrl);
            return;
        }

        try
        {
            using var client = new SmtpClient(_opts.Host, _opts.Port)
            {
                EnableSsl             = _opts.EnableSsl,
                Credentials           = new NetworkCredential(_opts.Username, _opts.Password),
                DeliveryMethod        = SmtpDeliveryMethod.Network,
            };
            using var message = new MailMessage(_opts.From, to, subject, htmlBody) { IsBodyHtml = true };
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка отправки письма на {Email}", to);
        }
    }
}

public sealed class SmtpOptions
{
    public bool Enabled { get; set; } = false;
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 25;
    public bool EnableSsl { get; set; } = false;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string From { get; set; } = "noreply@taverna.local";
    public string AppBaseUrl { get; set; } = "http://localhost:5141";
}
