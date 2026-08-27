using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CreateVendorDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactName { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(150)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }
}