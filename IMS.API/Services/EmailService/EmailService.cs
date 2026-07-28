using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace IMS.API.Services.EmailService;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        Console.WriteLine($"[EmailService DIAGNOSTIC START] To={toEmail} | Server={_settings.SmtpServer} | Port={_settings.Port} | Sender={_settings.SenderEmail} | User={_settings.Username}");

        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SenderName ?? "InvestPro", _settings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };

        Exception? lastException = null;

        // Attempt 1: Port 587 with StartTls
        try
        {
            Console.WriteLine($"[EmailService] Attempt 1: Connecting to {_settings.SmtpServer}:{_settings.Port} (StartTls)...");
            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
            Console.WriteLine($"[EmailService] Attempt 1: Connected. Authenticating as {_settings.Username}...");
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            Console.WriteLine($"[EmailService] Attempt 1: Authenticated. Sending email payload...");
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] Email sent to {toEmail} via Port {_settings.Port} (StartTls)");
            return;
        }
        catch (Exception ex)
        {
            lastException = ex;
            Console.WriteLine($"[EmailService ERROR - Attempt 1 Failed] {ex.GetType().FullName}: {ex.Message} | Inner: {ex.InnerException?.Message}");
        }

        // Attempt 2: Port 465 with SslOnConnect
        try
        {
            Console.WriteLine($"[EmailService] Attempt 2: Connecting to {_settings.SmtpServer}:465 (SslOnConnect)...");
            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync(_settings.SmtpServer, 465, SecureSocketOptions.SslOnConnect);
            Console.WriteLine($"[EmailService] Attempt 2: Connected to 465. Authenticating...");
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            Console.WriteLine($"[EmailService] Attempt 2: Authenticated. Sending email payload...");
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] Email sent to {toEmail} via Port 465 (SSL)");
            return;
        }
        catch (Exception ex)
        {
            lastException = ex;
            Console.WriteLine($"[EmailService ERROR - Attempt 2 Failed] {ex.GetType().FullName}: {ex.Message} | Inner: {ex.InnerException?.Message}");
        }

        // Attempt 3: Port 587 Auto
        try
        {
            Console.WriteLine($"[EmailService] Attempt 3: Connecting to {_settings.SmtpServer}:{_settings.Port} (Auto)...");
            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.Auto);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] Email sent to {toEmail} via Port {_settings.Port} (Auto)");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService ERROR - Attempt 3 Failed] {ex.GetType().FullName}: {ex.Message} | Inner: {ex.InnerException?.Message}");
            throw lastException ?? ex;
        }
    }
}
