namespace WMS.Application.DTOs;

public class StatusHistoryQueryDto
{
    public string? EntityType { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
