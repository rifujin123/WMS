using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class PurchaseOrderDto {
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? WarehouseCode { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<PurchaseOrderDetailDto> PurchaseOrderDetails { get; set; } = new();
}
