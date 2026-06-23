using System.ComponentModel.DataAnnotations;

namespace IMS.API.DTOs.Auth;

public class LoginDTO
{
    [Required]
    public string? Email { get; set; }

    [Required]
    public string? Password { get; set; }
}
