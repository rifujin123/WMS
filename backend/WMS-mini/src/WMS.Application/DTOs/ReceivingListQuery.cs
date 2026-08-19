using WMS.Domain.Enums;

namespace WMS.Application.DTOs;

public class ReceivingListQuery
{
    public int Page { get; init; } = 1;
    public string? Search { get; init; }
    public ReceivingStatus? Status { get; init; }
}
