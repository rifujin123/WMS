using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class StockMovementQueryDto
{
    public Guid? ProductId { get; set; }
    public Guid? LocationId { get; set; }
    public MovementType? MovementType { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
