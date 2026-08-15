namespace WMS.Application.DTOs;

public class StatusHistoryDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? Notes { get; set; }
    public string? MetadataJson { get; set; }
}
