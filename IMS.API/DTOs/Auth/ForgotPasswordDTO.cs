using System.ComponentModel.DataAnnotations;

namespace IMS.API.DTOs.Auth;

public class ForgotPasswordDTO
{
    [Required] 
    public string? Email { get; set; }
}
