using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class StockAdjustmentDto
{
    public Guid Id { get; set; }
    public string AdjustmentNo { get; set; } = string.Empty;
    public StockAdjustmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<StockAdjustmentDetailDto> Details { get; set; } = new();
}