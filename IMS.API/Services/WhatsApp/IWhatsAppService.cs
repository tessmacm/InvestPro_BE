namespace IMS.API.Services.WhatsApp;

public interface IWhatsAppService
{
    Task<bool> SendTextMessageAsync(string toPhoneNumber, string message);
    Task<bool> SendOtpAsync(string toPhoneNumber, string otp, string purpose = "verification");
    Task<bool> SendRegistrationWelcomeAsync(string toPhoneNumber, string name, string email);
    Task<bool> SendPayoutNotificationAsync(string toPhoneNumber, string investorName, decimal amount, string month, string date);
    Task<bool> SendGeneralNotificationAsync(string toPhoneNumber, string title, string message);
}
