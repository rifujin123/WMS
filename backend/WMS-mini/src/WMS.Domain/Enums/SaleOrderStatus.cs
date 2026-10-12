namespace WMS.Domain.Enums;

public enum SaleOrderStatus
{
    New,
    Allocated,
    Picking,
    Packed,
    Shipped,
    AwaitingDeposit,
    Backorder,
    Cancelled
}
