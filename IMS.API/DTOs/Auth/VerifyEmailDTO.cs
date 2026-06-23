using System.ComponentModel.DataAnnotations;

namespace IMS.API.DTOs.Auth;

public class VerifyEmailDTO
{
    [Required]
    public string? Email { get; set; }
    [Required]
    public string? OTP { get; set; }
}
