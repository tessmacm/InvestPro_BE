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

        // Force IPv4 address resolution to prevent Windows IPv6 socket read failures ("The read operation failed")
        string hostToConnect = _settings.SmtpServer;
        try
        {
            var addresses = await System.Net.Dns.GetHostAddressesAsync(_settings.SmtpServer);
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            if (ipv4 != null)
            {
                hostToConnect = ipv4.ToString();
                Console.WriteLine($"[EmailService] Resolved IPv4 address for {_settings.SmtpServer}: {hostToConnect}");
            }
        }
        catch (Exception dnsEx)
        {
            Console.WriteLine($"[EmailService WARNING] DNS resolution fallback: {dnsEx.Message}");
        }

        // Attempt 1: Try Port 587 with StartTls over IPv4
        try
        {
            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            smtp.CheckCertificateRevocation = false;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            Console.WriteLine($"[EmailService] Attempting dispatch to {toEmail} via {hostToConnect}:{_settings.Port} (StartTls)...");
            await smtp.ConnectAsync(hostToConnect, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {hostToConnect}:{_settings.Port} (StartTls)");
            return;
        }
        catch (Exception ex)
        {
            lastException = ex;
            Console.WriteLine($"[EmailService WARNING] Port {_settings.Port} StartTls failed: {ex.GetType().Name} - {ex.Message}. Trying Port 465 SSL...");
        }

        // Attempt 2: Try Port 465 with SslOnConnect over IPv4
        try
        {
            using var smtp = new SmtpClient();
            smtp.Timeout = 15000;
            smtp.CheckCertificateRevocation = false;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync(hostToConnect, 465, SecureSocketOptions.SslOnConnect);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {hostToConnect}:465 (SSL)");
            return;
        }
        catch (Exception ex)
        {
            lastException = ex;
            Console.WriteLine($"[EmailService WARNING] Port 465 SSL failed: {ex.GetType().Name} - {ex.Message}. Trying hostname direct...");
        }

        // Attempt 3: Direct Hostname Fallback
        try
        {
            using var smtpFallback = new SmtpClient();
            smtpFallback.Timeout = 15000;
            smtpFallback.CheckCertificateRevocation = false;
            smtpFallback.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtpFallback.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.Auto);
            await smtpFallback.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtpFallback.SendAsync(email);
            await smtpFallback.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {_settings.SmtpServer}:{_settings.Port} (Hostname Auto)");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService ERROR] All connection attempts failed for {toEmail}. Final error: {ex.GetType().Name} - {ex.Message}");
            throw lastException ?? ex;
        }
    }
}
