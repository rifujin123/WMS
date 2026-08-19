namespace WMS.Application.DTOs;

public class StockSummaryQuery
{
    public int Page { get; init; } = 1;
    public string? Search { get; init; }
    public Guid? LocationId { get; init; }
}
