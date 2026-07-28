namespace IMS.API.Services.EmailService;

public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string body);
    Task SendEmailWithAttachmentAsync(string toEmail, string subject, string body, string attachmentFileName, byte[] attachmentBytes, string contentType = "application/pdf");
}
