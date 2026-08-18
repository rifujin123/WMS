using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class SaleOrderDto
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public SaleOrderStatus Status { get; set; }
    public List<SaleOrderDetailDto> SaleOrderDetails { get; set; } = new();
}
