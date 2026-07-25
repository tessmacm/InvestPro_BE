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
        try
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(toEmail));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = body };

            using var smtp = new SmtpClient();
            // Connect with 10s timeout
            smtp.Timeout = 10000;
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] OTP email dispatched to {toEmail} via {_settings.SmtpServer}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService ERROR] Failed sending email to {toEmail} via {_settings.SmtpServer}:{_settings.Port}. Reason: {ex.GetType().Name} - {ex.Message}");
            throw; // Propagate so caller knows SMTP failed
        }
    }
}
