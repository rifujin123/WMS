using System.ComponentModel.DataAnnotations;

namespace WMS.Application.DTOs;

public class CreateBackorderLinkDto
{
    [Required]
    public Guid SaleOrderDetailId { get; set; }

    [Required]
    public Guid PurchaseOrderDetailId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Số lượng liên kết phải lớn hơn 0.")]
    public int Quantity { get; set; }
}
