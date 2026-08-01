using System.ComponentModel.DataAnnotations;
using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class CreateReceivingDto
{
    [Required]
    public Guid PurchaseOrderId { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateReceivingDetailDto> Details { get; set; } = new();

    [MaxLength(500)]
    public string? Notes { get; set; }
}
