namespace WMS.Application.DTOs;

public class CreateReceivingDto
{
    public Guid PurchaseOrderId { get; set; }
    public string? Notes { get; set; }
    public List<CreateReceivingDetailDto> ReceivingDetails { get; set; } = new();
}
