using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class StockMovementDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid LocationId { get; set; }
    public string LocationCode { get; set; } = string.Empty;
    public MovementType MovementType { get; set; }
    public int Qty { get; set; }
    public string? Notes { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public Guid? ActorUserId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? ActorAvatarUrl { get; set; }
}
