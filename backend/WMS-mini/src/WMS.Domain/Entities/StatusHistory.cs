using System.ComponentModel.DataAnnotations;

namespace WMS.Domain.Entities;

public class StatusHistory
{
    private StatusHistory() { }

    public StatusHistory(
        Guid id,
        string entityType,
        Guid entityId,
        string? fromStatus,
        string toStatus,
        string action,
        Guid? actorUserId,
        DateTime occurredAtUtc,
        string? notes = null,
        string? metadataJson = null)
    {
        Id = id;
        EntityType = entityType;
        EntityId = entityId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        Action = action;
        ActorUserId = actorUserId;
        OccurredAtUtc = occurredAtUtc;
        Notes = notes;
        MetadataJson = metadataJson;
    }

    public Guid Id { get; private set; }

    [MaxLength(200)]
    public string EntityType { get; private set; } = string.Empty;

    public Guid EntityId { get; private set; }

    [MaxLength(100)]
    public string? FromStatus { get; private set; }

    [MaxLength(100)]
    public string ToStatus { get; private set; } = string.Empty;

    [MaxLength(100)]
    public string Action { get; private set; } = string.Empty;

    public Guid? ActorUserId { get; private set; }
    public User? ActorUser { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    [MaxLength(1000)]
    public string? Notes { get; private set; }

    public string? MetadataJson { get; private set; }
}
