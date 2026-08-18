using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CreateShipmentDto
{
    [Required]
    public Guid SaleOrderId { get; set; }

    [MaxLength(100)]
    public string? Carrier { get; set; }

    [MaxLength(100)]
    public string? TrackingNo { get; set; }
}
