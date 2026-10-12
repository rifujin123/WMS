using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class SaleOrderDto
{
    public Guid Id { get; set; }
    public string OrderNo { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime OrderDate { get; set; }
    public SaleOrderStatus Status { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? WarehouseCode { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsPreOrder { get; set; }
    public decimal RequiredDeposit { get; set; }
    public decimal PaidDeposit { get; set; }
    public DateTime? DepositDeadline { get; set; }
    public string? DepositNotes { get; set; }
    public List<SaleOrderDetailDto> SaleOrderDetails { get; set; } = new();
}
