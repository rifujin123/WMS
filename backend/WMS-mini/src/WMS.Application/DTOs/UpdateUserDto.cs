using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class UpdateUserDto
{
    [Required]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;
}
