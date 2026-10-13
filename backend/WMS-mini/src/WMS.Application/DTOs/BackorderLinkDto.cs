namespace WMS.Application.DTOs;

public class BackorderLinkDto
{
    public Guid Id { get; set; }
    public Guid SaleOrderDetailId { get; set; }
    public Guid SaleOrderId { get; set; }
    public string SaleOrderNo { get; set; } = string.Empty;
    public Guid PurchaseOrderDetailId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string PurchaseOrderNo { get; set; } = string.Empty;
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int FulfilledQuantity { get; set; }
    public bool IsFulfilled { get; set; }
    public DateTime CreatedDate { get; set; }
}
