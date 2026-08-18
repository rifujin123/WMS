using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CreateSaleOrderDto
{
    [Required]
    [MaxLength(50)]
    public string OrderNo { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CustomerName { get; set; }

    public DateTime OrderDate { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateSaleOrderDetailDto> SaleOrderDetails { get; set; } = new();
}
