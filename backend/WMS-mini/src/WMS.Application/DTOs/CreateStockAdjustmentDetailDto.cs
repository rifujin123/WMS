using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CreateStockAdjustmentDetailDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid LocationId { get; set; }

    [Range(1, int.MaxValue)]
    public int CountedQty { get; set; }
}