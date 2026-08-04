using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CreateStockAdjustmentDto
{
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateStockAdjustmentDetailDto> Details { get; set; } = new();
}