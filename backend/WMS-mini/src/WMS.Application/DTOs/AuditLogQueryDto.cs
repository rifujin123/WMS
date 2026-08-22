namespace WMS.Application.DTOs;

public class AuditLogQueryDto
{
    public int Page { get; set; } = 1;
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public Guid? ActorId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}
