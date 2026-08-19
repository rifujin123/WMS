using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class Receiving : BaseAuditableEntity
{
    [Required]
    [MaxLength(50)]
    public string ReceivingNo { get; set; } = string.Empty;

    public Guid PurchaseOrderId { get; set; }

    [Required]
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid? ReceivedById { get; set; }
    public User? ReceivedBy { get; set; }
    public DateTime ReceivedDate { get; set; }
    public Guid? ConfirmedById { get; set; }
    public User? ConfirmedBy { get; set; }
    public DateTime? ConfirmedDate { get; set; }
    public ReceivingStatus Status { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(1000)]
    public string? InvoiceImageUrl { get; set; }

    public ICollection<ReceivingDetail> ReceivingDetails { get; set; } = new List<ReceivingDetail>();
}
