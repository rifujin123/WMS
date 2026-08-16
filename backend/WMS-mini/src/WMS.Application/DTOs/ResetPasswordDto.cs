using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class ResetPasswordDto
{
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = string.Empty;
}
