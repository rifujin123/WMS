using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class PurchaseOrder : BaseAuditableEntity
{
    [MaxLength(50)]
    public string PoNumber { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? VendorName { get; set; }

    public PurchaseOrderStatus Status { get; set; }

    public Guid? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTime? ApprovedDate { get; set; }

    public ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
    public ICollection<Receiving> Receivings { get; set; } = new List<Receiving>();
}
