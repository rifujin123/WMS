using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class VendorDebtLedger : BaseAuditableEntity
{
    public Guid? VendorId { get; set; }
    public Vendor? Vendor { get; set; }

    [MaxLength(200)]
    public string? VendorName { get; set; }

    public Guid SourceReceivingId { get; set; }
    public Receiving SourceReceiving { get; set; } = null!;

    public VendorDebtType DebtType { get; set; }

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public int? Quantity { get; set; }

    public decimal? Amount { get; set; }

    public bool IsResolved { get; set; }

    public Guid? ResolvedInPoId { get; set; }
    public PurchaseOrder? ResolvedInPo { get; set; }

    public DateTime? ResolvedDate { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }
}
