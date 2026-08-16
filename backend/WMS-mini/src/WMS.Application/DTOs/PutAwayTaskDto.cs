using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class PutAwayTaskDto
{
    public Guid Id { get; set; }
    public Guid ReceivingDetailId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public Guid? FromLocationId { get; set; }
    public string? FromLocationCode { get; set; }
    public Guid? ToLocationId { get; set; }
    public string? ToLocationCode { get; set; }
    public PutAwayTaskStatus Status { get; set; }
    public Guid? AssignToId { get; set; }
    public string? AssignToName { get; set; }
    public string? AssignToAvatarUrl { get; set; }
    public DateTime CreatedDate { get; set; }
}
