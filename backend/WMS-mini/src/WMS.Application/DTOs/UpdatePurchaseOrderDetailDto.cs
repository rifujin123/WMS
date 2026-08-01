namespace WMS.Application.DTOs;

public class UpdatePurchaseOrderDetailDto
{
    public Guid ProductId { get; set; }
    public int OrderedQuantity { get; set; }
}
