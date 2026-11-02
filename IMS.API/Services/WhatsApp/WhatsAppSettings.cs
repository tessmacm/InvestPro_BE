using System.Text.Json.Serialization;

namespace IMS.API.Services.WhatsApp;

public class WhatsAppSettings
{
    public string Token { get; set; } = string.Empty;
    public string PhoneNumberId { get; set; } = string.Empty;
    public string GraphVersion { get; set; } = "v25.0";
    public bool Enabled { get; set; } = true;
    public string DefaultRecipientNumber { get; set; } = "+919885255404";
}
