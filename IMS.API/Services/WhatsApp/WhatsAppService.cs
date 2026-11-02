using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IMS.API.Services.WhatsApp;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        HttpClient httpClient,
        IOptions<WhatsAppSettings> options,
        IConfiguration configuration,
        ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = options.Value ?? new WhatsAppSettings();

        // Fallback to environment variables or config keys if not provided in section
        if (string.IsNullOrWhiteSpace(_settings.Token))
        {
            _settings.Token = configuration["META_WHATSAPP_TOKEN"] 
                           ?? configuration["WhatsAppSettings:Token"] 
                           ?? Environment.GetEnvironmentVariable("META_WHATSAPP_TOKEN") 
                           ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            _settings.PhoneNumberId = configuration["META_PHONE_NUMBER_ID"] 
                                   ?? configuration["WhatsAppSettings:PhoneNumberId"] 
                                   ?? Environment.GetEnvironmentVariable("META_PHONE_NUMBER_ID") 
                                   ?? "1362087393659173";
        }

        if (string.IsNullOrWhiteSpace(_settings.GraphVersion))
        {
            _settings.GraphVersion = configuration["META_GRAPH_VERSION"] 
                                  ?? configuration["WhatsAppSettings:GraphVersion"] 
                                  ?? Environment.GetEnvironmentVariable("META_GRAPH_VERSION") 
                                  ?? "v25.0";
        }
    }

    private string NormalizePhoneNumber(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        // Strip +, spaces, dashes, parentheses
        var clean = Regex.Replace(phone, @"[^\d]", "");
        return clean;
    }

    public async Task<bool> SendTextMessageAsync(string toPhoneNumber, string message)
    {
        var cleanedPhone = NormalizePhoneNumber(toPhoneNumber);
        if (string.IsNullOrWhiteSpace(cleanedPhone))
        {
            _logger.LogWarning("[WhatsApp] Cannot send message: recipient phone number is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Token) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            _logger.LogWarning("[WhatsApp] WhatsApp settings (Token or PhoneNumberId) are not configured.");
            return false;
        }

        var endpoint = $"https://graph.facebook.com/{_settings.GraphVersion}/{_settings.PhoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = cleanedPhone,
            type = "text",
            text = new
            {
                preview_url = false,
                body = message
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[WhatsApp] Message sent successfully to {Phone}: {Response}", cleanedPhone, responseContent);
                return true;
            }
            else
            {
                _logger.LogError("[WhatsApp] Failed to send message to {Phone}. Status: {Status}, Details: {Response}", 
                    cleanedPhone, response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WhatsApp] Exception while sending message to {Phone}", cleanedPhone);
            return false;
        }
    }

    public async Task<bool> SendTemplateAsync(string toPhoneNumber, string templateName, string languageCode, object[] components)
    {
        var cleanedPhone = NormalizePhoneNumber(toPhoneNumber);
        if (string.IsNullOrWhiteSpace(cleanedPhone))
        {
            _logger.LogWarning("[WhatsApp] Cannot send template: recipient phone number is empty.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Token) || string.IsNullOrWhiteSpace(_settings.PhoneNumberId))
        {
            _logger.LogWarning("[WhatsApp] WhatsApp settings are not configured.");
            return false;
        }

        var endpoint = $"https://graph.facebook.com/{_settings.GraphVersion}/{_settings.PhoneNumberId}/messages";

        var payload = new
        {
            messaging_product = "whatsapp",
            recipient_type = "individual",
            to = cleanedPhone,
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = languageCode },
                components
            }
        };

        try
        {
            var json = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("[WhatsApp] Template {Template} sent successfully to {Phone}: {Response}", templateName, cleanedPhone, responseContent);
                return true;
            }
            else
            {
                _logger.LogError("[WhatsApp] Failed to send template {Template} to {Phone}. Status: {Status}, Details: {Response}", 
                    templateName, cleanedPhone, response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WhatsApp] Exception while sending template {Template} to {Phone}", templateName, cleanedPhone);
            return false;
        }
    }

    public async Task<bool> SendOtpAsync(string toPhoneNumber, string otp, string purpose = "verification")
    {
        // 1. First attempt sending via approved Meta AUTHENTICATION template: account_login_alert
        var components = new object[]
        {
            new
            {
                type = "body",
                parameters = new object[]
                {
                    new { type = "text", text = otp }
                }
            },
            new
            {
                type = "button",
                sub_type = "url",
                index = "0",
                parameters = new object[]
                {
                    new { type = "text", text = otp }
                }
            }
        };

        var templateSent = await SendTemplateAsync(toPhoneNumber, "account_login_alert", "en", components);
        if (templateSent) return true;

        // 2. Fallback to direct custom text message if template fails
        _logger.LogWarning("[WhatsApp] Template dispatch failed, falling back to direct text message for OTP.");
        var message = $"🔒 *InvestPro {purpose.ToUpperInvariant()} CODE*\n\n" +
                      $"Your one-time code is: *{otp}*\n\n" +
                      $"This code is valid for 10 minutes. Do not share this OTP with anyone for your account security.";
        return await SendTextMessageAsync(toPhoneNumber, message);
    }

    public async Task<bool> SendRegistrationWelcomeAsync(string toPhoneNumber, string name, string email)
    {
        // 1. Attempt sending via approved UTILITY template: account_registration_success
        var components = new object[]
        {
            new
            {
                type = "body",
                parameters = new object[]
                {
                    new { type = "text", text = name },
                    new { type = "text", text = email }
                }
            }
        };

        var templateSent = await SendTemplateAsync(toPhoneNumber, "account_registration_success", "en", components);
        if (templateSent) return true;

        // 2. Fallback to direct custom text message
        var message = $"👋 *Welcome to InvestPro / Tessma*\n\n" +
                      $"Hi {name}, your account has been successfully created with email: {email}.\n\n" +
                      $"Thank you for joining our platform!";
        return await SendTextMessageAsync(toPhoneNumber, message);
    }

    public Task<bool> SendPayoutNotificationAsync(string toPhoneNumber, string investorName, decimal amount, string month, string date)
    {
        var message = $"💰 *InvestPro Payout Confirmation*\n\n" +
                      $"Dear {investorName},\n\n" +
                      $"We are pleased to inform you that your payout of *£{amount:N2}* for *{month}* has been successfully processed on *{date}*.\n\n" +
                      $"Thank you for investing with InvestPro.";
        return SendTextMessageAsync(toPhoneNumber, message);
    }

    public Task<bool> SendGeneralNotificationAsync(string toPhoneNumber, string title, string message)
    {
        var text = $"📢 *InvestPro Notification: {title}*\n\n{message}";
        return SendTextMessageAsync(toPhoneNumber, text);
    }
}
