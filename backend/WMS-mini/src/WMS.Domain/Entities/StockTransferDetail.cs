using System.ComponentModel.DataAnnotations;
using WMS.Domain.Common;

namespace WMS.Domain.Entities;

public class StockTransferDetail : BaseAuditableEntity
{
    public Guid StockTransferId { get; set; }
    public StockTransfer StockTransfer { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid FromLocationId { get; set; }
    public Location FromLocation { get; set; } = null!;

    public Guid? ToLocationId { get; set; }
    public Location? ToLocation { get; set; }

    public int TransferQty { get; set; }
    public int ReceivedQty { get; set; }
}
