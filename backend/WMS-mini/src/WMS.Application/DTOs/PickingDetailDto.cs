using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class PickingDetailDto
{
    public Guid Id { get; set; }
    public Guid PickingId { get; set; }
    public Guid? SaleOrderDetailId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid? LocationId { get; set; }
    public string? LocationCode { get; set; }
    public int QtyToPick { get; set; }
    public int QtyPicked { get; set; }
    public PickingDetailStatus Status { get; set; }
}
