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
            smtp.Timeout = 15000;
            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            Console.WriteLine($"[EmailService] Dispatching email to {toEmail} via {_settings.SmtpServer}:{_settings.Port}...");
            await smtp.ConnectAsync(_settings.SmtpServer, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);

            Console.WriteLine($"[EmailService SUCCESS] Email sent successfully to {toEmail}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EmailService ERROR] Failed to send email to {toEmail}: {ex.GetType().Name} - {ex.Message}");
            throw;
        }
    }
}
