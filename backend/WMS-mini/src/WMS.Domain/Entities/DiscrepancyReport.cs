using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class DiscrepancyReport : BaseAuditableEntity
{
    public Guid ReceivingId { get; set; }

    [Required]
    public Receiving Receiving { get; set; } = null!;

    public Guid PurchaseOrderId { get; set; }

    [Required]
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    [MaxLength(200)]
    public string? VendorName { get; set; }

    public DiscrepancyReportStatus Status { get; set; } = DiscrepancyReportStatus.Pending;

    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }

    [MaxLength(2000)]
    public string? ResolutionSummary { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    [MaxLength(2000)]
    public string? EvidenceImagesJson { get; set; }
}
