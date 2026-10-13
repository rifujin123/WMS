using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class VendorDebtLedgerDto
{
    public Guid Id { get; set; }
    public Guid? VendorId { get; set; }
    public string? VendorName { get; set; }
    public Guid SourceReceivingId { get; set; }
    public string? SourceReceivingNo { get; set; }
    public string? SourcePoNumber { get; set; }
    public VendorDebtType DebtType { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductSku { get; set; }
    public string? ProductName { get; set; }
    public int? Quantity { get; set; }
    public decimal? Amount { get; set; }
    public bool IsResolved { get; set; }
    public Guid? ResolvedInPoId { get; set; }
    public string? ResolvedInPoNumber { get; set; }
    public DateTime? ResolvedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string? Notes { get; set; }
}
