namespace WMS.Application.DTOs;

public class AuditLogDto
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? ActorAvatarUrl { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public string? ChangedFieldsJson { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestPath { get; set; }
}
