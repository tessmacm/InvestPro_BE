using IMS.API.Services.WhatsApp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IMS.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class WhatsAppTestController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IConfiguration _configuration;

    public WhatsAppTestController(IWhatsAppService whatsAppService, IConfiguration configuration)
    {
        _whatsAppService = whatsAppService;
        _configuration = configuration;
    }

    [HttpPost("test-message")]
    public async Task<IActionResult> TestMessage([FromBody] WhatsAppTestRequest? request)
    {
        var phone = request?.PhoneNumber;
        if (string.IsNullOrWhiteSpace(phone))
        {
            phone = _configuration["WhatsAppSettings:DefaultRecipientNumber"] ?? "+919885255404";
        }

        var message = request?.Message ?? "Hello from InvestPro! WhatsApp integration test message successful.";
        var success = await _whatsAppService.SendTextMessageAsync(phone, message);

        if (success)
        {
            return Ok(new
            {
                success = true,
                message = "WhatsApp message dispatched successfully.",
                recipient = phone
            });
        }

        return BadRequest(new
        {
            success = false,
            message = "Failed to dispatch WhatsApp message. Check server logs or verify recipient is in Meta allowed list (for sandbox/development apps).",
            recipient = phone
        });
    }

    [HttpPost("test-otp")]
    public async Task<IActionResult> TestOtp([FromQuery] string? phone, [FromQuery] string? otp = "123456")
    {
        var targetPhone = !string.IsNullOrWhiteSpace(phone) 
            ? phone 
            : (_configuration["WhatsAppSettings:DefaultRecipientNumber"] ?? "+919885255404");

        var success = await _whatsAppService.SendOtpAsync(targetPhone, otp ?? "123456", "login");
        return Ok(new { success, recipient = targetPhone, otp });
    }

    [HttpPost("test-payout")]
    public async Task<IActionResult> TestPayout([FromQuery] string? phone, [FromQuery] decimal amount = 1500m)
    {
        var targetPhone = !string.IsNullOrWhiteSpace(phone) 
            ? phone 
            : (_configuration["WhatsAppSettings:DefaultRecipientNumber"] ?? "+919885255404");

        var success = await _whatsAppService.SendPayoutNotificationAsync(
            targetPhone, 
            "Shaik", 
            amount, 
            DateTime.UtcNow.ToString("MMMM yyyy"), 
            DateTime.UtcNow.ToString("dd/MM/yyyy, HH:mm"));

        return Ok(new { success, recipient = targetPhone, amount });
    }
}

public class WhatsAppTestRequest
{
    public string? PhoneNumber { get; set; }
    public string? Message { get; set; }
}
