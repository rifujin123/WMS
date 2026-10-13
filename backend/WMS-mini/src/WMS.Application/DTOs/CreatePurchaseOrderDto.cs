namespace WMS.Application.DTOs;

public class CreatePurchaseOrderDto {
    public string PoNumber { get; set; } = string.Empty;
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public decimal CreditOffsetAmount { get; set; }
    public Guid? WarehouseId { get; set; }
    public List<Guid> ResolvedDebtIds { get; set; } = new();
    public List<CreatePurchaseOrderDetailDto> PurchaseOrderDetails { get; set; } = new();
}
