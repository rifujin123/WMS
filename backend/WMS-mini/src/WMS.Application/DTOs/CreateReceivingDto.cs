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

    [MaxLength(1000)]
    public string? InvoiceImageUrl { get; set; }

    [MaxLength(100)]
    public string? InvoiceNumber { get; set; }

    public DateTime? InvoiceDate { get; set; }

    public decimal? InvoiceTotalAmount { get; set; }
}
