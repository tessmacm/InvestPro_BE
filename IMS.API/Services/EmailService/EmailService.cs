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

        var fullHtmlBody = body.Contains("<!DOCTYPE html>") ? body : $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>{subject}</title>
</head>
<body style=""font-family: Arial, sans-serif; background-color: #f8fafc; margin: 0; padding: 20px;"">
  <div style=""max-width: 540px; margin: 0 auto; background: #ffffff; padding: 32px; border-radius: 16px; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.05);"">
    <div style=""text-align: center; margin-bottom: 24px;"">
      <span style=""font-size: 20px; font-weight: 800; color: #1e3a8a; font-family: sans-serif;"">Tessma Group | InvestPro</span>
    </div>
    <div style=""color: #334155; font-size: 15px; line-height: 1.6;"">
      {body}
    </div>
    <hr style=""border: none; border-top: 1px solid #e2e8f0; margin: 28px 0 16px 0;"" />
    <p style=""color: #94a3b8; font-size: 12px; text-align: center; margin: 0;"">
      This is an automated system notification from InvestPro Platform. Please do not reply to this email.
    </p>
  </div>
</body>
</html>";

        email.Body = new TextPart("html") { Text = fullHtmlBody };

        Exception? lastException = null;

        // Attempt 1: Try Port 465 with SslOnConnect (Fastest & Most Reliable for Gmail SMTP)
        try
        {
            using var smtp = new SmtpClient();
            smtp.Timeout = 10000;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            Console.WriteLine($"[EmailService] Attempting dispatch to {toEmail} via {_settings.SmtpServer}:465 (SslOnConnect)...");
            await smtp.ConnectAsync(_settings.SmtpServer, 465, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {_settings.SmtpServer}:465 (SSL)");
            return;
        }
        catch (Exception ex)
        {
            lastException = ex;
            Console.WriteLine($"[EmailService WARNING] Port 465 SSL failed: {ex.GetType().Name} - {ex.Message}. Trying Port {_settings.Port} (StartTls)...");
        }

        // Attempt 2: Try Port 587 with StartTls
        try
        {
            using var smtp = new SmtpClient();
            smtp.Timeout = 10000;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {_settings.SmtpServer}:{_settings.Port} (StartTls)");
            return;
        }
        catch (Exception ex)
        {
            lastException = ex;
            Console.WriteLine($"[EmailService WARNING] Port {_settings.Port} StartTls failed: {ex.GetType().Name} - {ex.Message}. Trying Auto...");
        }

        // Attempt 3: Fallback to Port 587 Auto
        try
        {
            using var smtpFallback = new SmtpClient();
            smtpFallback.Timeout = 10000;
            smtpFallback.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtpFallback.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.Auto);
            await smtpFallback.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtpFallback.SendAsync(email);
            await smtpFallback.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {_settings.SmtpServer}:{_settings.Port} (Auto)");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService ERROR] All connection attempts failed for {toEmail}. Final error: {ex.GetType().Name} - {ex.Message}");
            throw lastException ?? ex;
        }
    }
}
