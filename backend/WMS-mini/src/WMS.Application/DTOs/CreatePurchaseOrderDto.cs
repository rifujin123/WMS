namespace WMS.Application.DTOs;

public class CreatePurchaseOrderDto {
    public string PoNumber { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public List<CreatePurchaseOrderDetailDto> PurchaseOrderDetails { get; set; } = new();
}
