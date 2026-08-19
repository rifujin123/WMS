using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class PurchaseOrderListQuery
{
    public int Page { get; init; } = 1;
    public string? Search { get; init; }
    public PurchaseOrderStatus? Status { get; init; }
}
