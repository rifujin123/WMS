using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class PickingDto
{
    public Guid Id { get; set; }
    public string PickingNo { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public PickingStatus Status { get; set; }
    public Guid? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<PickingDetailDto> Details { get; set; } = new();
}
