using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;
using WMS.Domain.Enums;

namespace WMS.Domain.Entities;

public class StockTransfer : BaseAuditableEntity
{
    [MaxLength(50)]
    public string TransferNo { get; set; } = string.Empty;

    public Guid FromWarehouseId { get; set; }
    public Warehouse FromWarehouse { get; set; } = null!;

    public Guid ToWarehouseId { get; set; }
    public Warehouse ToWarehouse { get; set; } = null!;

    public StockTransferStatus Status { get; set; } = StockTransferStatus.Draft;

    public DateTime? ShippedDateUtc { get; set; }
    public DateTime? ReceivedDateUtc { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public ICollection<StockTransferDetail> Details { get; set; } = new List<StockTransferDetail>();
}
