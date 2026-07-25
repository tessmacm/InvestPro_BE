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
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
        email.To.Add(MailboxAddress.Parse(toEmail));
        email.Subject = subject;
        email.Body = new TextPart("html") { Text = body };

        Exception? lastException = null;

        // Attempt 1: Try configured port (default 587 with StartTls)
        try
        {
            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true; // Prevent SSL cert validation errors in dev

            Console.WriteLine($"[EmailService] Attempting dispatch to {toEmail} via {_settings.SmtpServer}:{_settings.Port} (StartTls)...");
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {_settings.SmtpServer}:{_settings.Port}");
            return;
        }
        catch (Exception ex)
        {
            lastException = ex;
            Console.WriteLine($"[EmailService WARNING] Port {_settings.Port} StartTls failed: {ex.GetType().Name} - {ex.Message}. Trying fallback Port 465 (SslOnConnect)...");
        }

        // Attempt 2: Fallback to Port 465 with SslOnConnect
        try
        {
            using var smtpFallback = new SmtpClient();
            smtpFallback.Timeout = 15000;
            smtpFallback.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtpFallback.ConnectAsync(_settings.SmtpServer, 465, SecureSocketOptions.SslOnConnect);
            await smtpFallback.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtpFallback.SendAsync(email);
            await smtpFallback.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {_settings.SmtpServer}:465 (Fallback)");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService ERROR] Both Port 587 and Port 465 failed for {toEmail}. Final error: {ex.GetType().Name} - {ex.Message}");
            throw lastException ?? ex;
        }
    }
}
