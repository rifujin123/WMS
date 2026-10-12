using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class DiscrepancyReportDto
{
    public Guid Id { get; set; }
    public Guid ReceivingId { get; set; }
    public string ReceivingNo { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string? PoNumber { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public DiscrepancyReportStatus Status { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ResolutionSummary { get; set; }
    public string? Notes { get; set; }
    public string? EvidenceImagesJson { get; set; }
    public DateTime CreatedDate { get; set; }
}
