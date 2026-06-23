using System.ComponentModel.DataAnnotations;

namespace IMS.API.DTOs.Auth;

public class ResetPasswordDTO
{
    [Required]
    public string? Email { get; set; }

    [Required] 
    public string? Token { get; set; }

    [Required] 
    public string? NewPassword { get; set; }

}
