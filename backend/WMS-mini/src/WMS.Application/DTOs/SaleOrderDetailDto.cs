using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class SaleOrderDetailDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int AllocatedQty { get; set; }
    public SaleOrderDetailStatus Status { get; set; }
}
