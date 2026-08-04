using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class PurchaseOrderDto {
    public Guid Id { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public string? VendorName { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public List<PurchaseOrderDetailDto> PurchaseOrderDetails { get; set; } = new();
}
