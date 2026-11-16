using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Sentinel.Application.Notifications;

namespace Sentinel.Infrastructure.Notifications;

public sealed class SmtpEmailProvider(IOptions<EmailOptions> options) : IEmailProvider
{
    private readonly EmailOptions emailOptions = options.Value;

    public async Task SendAsync(IncidentNotification notification, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(emailOptions.Host) || string.IsNullOrWhiteSpace(emailOptions.From))
        {
            throw new InvalidOperationException("Email provider is not configured.");
        }

        using var message = new MailMessage(emailOptions.From, notification.Recipient)
        {
            Subject = notification.Subject,
            Body = notification.Body,
            IsBodyHtml = false
        };
        using var client = new SmtpClient(emailOptions.Host, emailOptions.Port)
        {
            EnableSsl = emailOptions.UseSsl
        };
        if (!string.IsNullOrWhiteSpace(emailOptions.Username))
        {
            client.Credentials = new NetworkCredential(emailOptions.Username, emailOptions.Password);
        }

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
