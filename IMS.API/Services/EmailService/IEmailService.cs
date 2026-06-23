namespace IMS.API.Services.EmailService;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
}
