using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class ReceivingDto
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string? PoNumber { get; set; }
    public Guid? ReceivedById { get; set; }
    public string? ReceivedByName { get; set; }
    public DateTime ReceivedDate { get; set; }
    public ReceivingStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<ReceivingDetailDto> Details { get; set; } = new();
    public DateTime CreatedDate { get; set; }
}
