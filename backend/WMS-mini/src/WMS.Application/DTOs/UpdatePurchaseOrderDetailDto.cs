namespace WMS.Application.DTOs;

public class UpdatePurchaseOrderDetailDto
{
    public Guid ProductId { get; set; }
    public int OrderedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TaxRate { get; set; }
}
