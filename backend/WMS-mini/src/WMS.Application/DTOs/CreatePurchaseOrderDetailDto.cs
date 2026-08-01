namespace WMS.Application.DTOs;

public class CreatePurchaseOrderDetailDto
{
    public Guid ProductId { get; set; }
    public int OrderedQuantity { get; set; }
}
