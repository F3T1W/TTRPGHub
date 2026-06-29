namespace TTRPGHub.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(string toEmail, string username, string confirmUrl, CancellationToken ct = default);
    Task SendPasswordResetAsync(string toEmail, string username, string resetUrl, CancellationToken ct = default);
}
