using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class UpdateWarehouseDto
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Address { get; set; }
}
