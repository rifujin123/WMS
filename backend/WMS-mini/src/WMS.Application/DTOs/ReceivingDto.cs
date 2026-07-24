using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class ReceivingDto
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public string PoNumber { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public ReceivingStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<ReceivingDetailDto> ReceivingDetails { get; set; } = new();
}
