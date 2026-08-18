using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class AuditLog
{
    private AuditLog() { }

    public AuditLog(
        Guid id,
        string entityType,
        Guid entityId,
        string action,
        Guid? actorUserId,
        DateTime occurredAtUtc,
        string? oldValuesJson = null,
        string? newValuesJson = null,
        string? changedFieldsJson = null,
        string? correlationId = null,
        string? requestPath = null)
    {
        Id = id;
        EntityType = entityType;
        EntityId = entityId;
        Action = action;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc;
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
        ChangedFieldsJson = changedFieldsJson;
        CorrelationId = correlationId;
        RequestPath = requestPath;
    }

    public Guid Id { get; private set; }

    [MaxLength(200)]
    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    [MaxLength(100)]
    public string Action { get; private set; } = string.Empty;

    public Guid? ActorUserId { get; private set; }
    public User? ActorUser { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public string? ChangedFieldsJson { get; private set; }

    [MaxLength(100)]
    public string? CorrelationId { get; private set; }

    [MaxLength(500)]
    public string? RequestPath { get; private set; }
}
